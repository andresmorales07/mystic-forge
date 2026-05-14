using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using MysticForge.Application.Spellbook;
using MysticForge.CommanderSpellbook.Generated;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Spellbook;
using MysticForge.IntegrationTests.Harness;
using WireMock.Server;

namespace MysticForge.IntegrationTests.Spellbook;

[Collection("postgres")]
public sealed class SpellbookRefreshJobTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private          DatabaseFixture          _db = null!;
    private          WireMockServer           _wm = null!;

    public SpellbookRefreshJobTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        _wm = WireMockServer.Start(new WireMock.Settings.WireMockServerSettings
        {
            StartAdminInterface = false,
        });

        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE spellbook_ingest_runs RESTART IDENTITY CASCADE");
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE cards RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync()
    {
        _wm.Dispose();
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    private SpellbookRefreshJob CreateJob(MysticForgeDbContext db)
    {
        var baseUrl = _wm.Url!.TrimEnd('/') + "/";
        var http    = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http);
        var kiota   = new SpellbookApiClient(adapter);

        var opts = Options.Create(new CommanderSpellbookOptions
        {
            BaseUrl                      = baseUrl,
            PageSize                     = 100,
            PerPageDelayMs               = 0,
            RefreshRequestTimeoutSeconds = 5,
        });

        var client  = new SpellbookRefreshClient(kiota, opts);
        var writer  = new ComboMirrorWriter(db, new CardNameResolver(db), TimeProvider.System);
        var tracker = new SpellbookIngestRunTracker(db, TimeProvider.System);
        var log     = NullLogger<SpellbookRefreshJob>.Instance;

        return new SpellbookRefreshJob(client, writer, tracker, log);
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Happy_path_refresh_populates_mirror()
    {
        SpellbookFixturePlayer.RegisterAll(_wm);

        await using var db = _db.NewContext();
        await CreateJob(db).RunAsync();

        // Run record
        var run = await db.SpellbookIngestRuns.OrderByDescending(r => r.RunId).FirstAsync();
        run.Outcome.Should().Be("success");
        run.VariantsSeen.Should().Be(3);
        run.FeaturesSeen.Should().Be(2);
        run.TemplatesSeen.Should().Be(1);
        run.CombosInserted.Should().Be(3);
        run.CombosUpdated.Should().Be(0);
        run.FeaturesInserted.Should().Be(2);
        run.FeaturesUpdated.Should().Be(0);
        run.TemplatesInserted.Should().Be(1);
        run.TemplatesUpdated.Should().Be(0);
        run.CombosSoftDeleted.Should().Be(0);

        // Per-card spot-check: Bloodghast should be in 1 combo
        var bloodghastCards = await db.ComboCards
            .Where(c => c.CardName == "Bloodghast")
            .ToListAsync();
        bloodghastCards.Should().HaveCount(1);
        bloodghastCards[0].ComboId.Should().Be("v-bloodghast-loop");
    }

    [Fact]
    public async Task Idempotent_re_run_produces_no_net_change()
    {
        SpellbookFixturePlayer.RegisterAll(_wm);

        // Run 1
        await using (var db1 = _db.NewContext())
            await CreateJob(db1).RunAsync();

        // Run 2 — re-register stubs (WireMock state reuse)
        SpellbookFixturePlayer.RegisterAll(_wm);
        await using (var db2 = _db.NewContext())
            await CreateJob(db2).RunAsync();

        await using var verify = _db.NewContext();
        var run2 = await verify.SpellbookIngestRuns.OrderByDescending(r => r.RunId).FirstAsync();
        run2.Outcome.Should().Be("success");
        run2.CombosInserted.Should().Be(0);
        run2.CombosUpdated.Should().Be(3);   // all 3 combos seen again → updated
        run2.FeaturesInserted.Should().Be(0);
        run2.FeaturesUpdated.Should().Be(2);
        run2.TemplatesInserted.Should().Be(0);
        run2.TemplatesUpdated.Should().Be(1);
        run2.CombosSoftDeleted.Should().Be(0);
    }

    [Fact]
    public async Task Soft_mark_marks_combo_missing_from_subsequent_run()
    {
        // Run A — full fixture
        SpellbookFixturePlayer.RegisterAll(_wm);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        // Run B — omit v-bloodghast-loop
        var subset = SpellbookFixturePlayer.Variants
            .Where(v => v.Id != "v-bloodghast-loop")
            .ToList();
        SpellbookFixturePlayer.RegisterVariantsSubset(_wm, subset);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        await using var verify = _db.NewContext();
        var run2 = await verify.SpellbookIngestRuns.OrderByDescending(r => r.RunId).FirstAsync();
        run2.Outcome.Should().Be("success");
        run2.CombosSoftDeleted.Should().BeGreaterThanOrEqualTo(1);

        var missing = await verify.Combos.FindAsync("v-bloodghast-loop");
        missing.Should().NotBeNull();
        missing!.DeletedAt.Should().NotBeNull("combo absent from second run should be soft-deleted");
    }

    [Fact]
    public async Task Re_emergence_clears_deleted_at()
    {
        // Run A — full fixture
        SpellbookFixturePlayer.RegisterAll(_wm);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        // Run B — subset (v-bloodghast-loop absent)
        var subset = SpellbookFixturePlayer.Variants
            .Where(v => v.Id != "v-bloodghast-loop")
            .ToList();
        SpellbookFixturePlayer.RegisterVariantsSubset(_wm, subset);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        // Verify it was soft-deleted
        await using (var check = _db.NewContext())
        {
            var c = await check.Combos.FindAsync("v-bloodghast-loop");
            c!.DeletedAt.Should().NotBeNull();
        }

        // Run C — full fixture again (v-bloodghast-loop re-appears)
        SpellbookFixturePlayer.RegisterAll(_wm);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        await using var verify = _db.NewContext();
        var restored = await verify.Combos.FindAsync("v-bloodghast-loop");
        restored.Should().NotBeNull();
        restored!.DeletedAt.Should().BeNull("re-emerged combo should have DeletedAt cleared");
    }

    [Fact]
    public async Task Partial_failure_skips_sweep()
    {
        // First run to populate some combos so soft-mark would have work to do
        SpellbookFixturePlayer.RegisterAll(_wm);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        // Second run: variants endpoint returns 500
        SpellbookFixturePlayer.RegisterVariants500(_wm);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        await using var verify = _db.NewContext();
        var run = await verify.SpellbookIngestRuns.OrderByDescending(r => r.RunId).FirstAsync();
        run.Outcome.Should().Be("partial");
        run.CombosSoftDeleted.Should().BeNull("soft-mark sweep must be skipped on partial failure");

        // All combos from run 1 should still be visible (not soft-deleted)
        var deletedCount = await verify.Combos.CountAsync(c => c.DeletedAt != null);
        deletedCount.Should().Be(0, "no combos should be soft-deleted when sweep is skipped");
    }

    [Fact]
    public async Task Resolution_catch_up_after_card_lands()
    {
        // Run 1: upsert combos before Bloodghast exists in the cards table.
        SpellbookFixturePlayer.RegisterAll(_wm);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        // oracle_id should be NULL because the card isn't in the DB yet.
        await using (var check = _db.NewContext())
        {
            var card = await check.ComboCards
                .Where(c => c.ComboId == "v-bloodghast-loop" && c.CardName == "Bloodghast")
                .FirstAsync();
            card.OracleId.Should().BeNull("card not yet seeded");
        }

        // Seed Bloodghast into the cards table.
        Guid bloodghastOracleId;
        await using (var db = _db.NewContext())
        {
            (bloodghastOracleId, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Bloodghast");
        }

        // Run 2: same fixture. The writer resolves oracle_id on re-upsert.
        SpellbookFixturePlayer.RegisterAll(_wm);
        await using (var db = _db.NewContext())
            await CreateJob(db).RunAsync();

        await using var verify = _db.NewContext();
        var resolved = await verify.ComboCards
            .Where(c => c.ComboId == "v-bloodghast-loop" && c.CardName == "Bloodghast")
            .FirstAsync();
        resolved.OracleId.Should().Be(bloodghastOracleId, "oracle_id should be resolved after card lands");
    }
}
