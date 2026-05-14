using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using MysticForge.Application.Spellbook;
using MysticForge.CommanderSpellbook.Generated;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Spellbook;
using MysticForge.IntegrationTests.Harness;
using WireMock.RequestBuilders;
using WireMock.Server;
using WmResponse = WireMock.ResponseBuilders.Response;

namespace MysticForge.IntegrationTests.Spellbook;

[Collection("postgres")]
public sealed class SpellbookFindMyCombosClientTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private          DatabaseFixture          _db = null!;
    private          WireMockServer           _wm = null!;

    public SpellbookFindMyCombosClientTests(PostgresContainerFixture pg) { _pg = pg; }

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
            "TRUNCATE TABLE find_my_combos_cache RESTART IDENTITY CASCADE");
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

    private SpellbookFindMyCombosClient CreateSut(MysticForgeDbContext db)
    {
        var baseUrl = _wm.Url!.TrimEnd('/') + "/";
        var http    = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http);
        var kiota   = new SpellbookApiClient(adapter);

        var tracker = new SpellbookIngestRunTracker(db, TimeProvider.System);

        return new SpellbookFindMyCombosClient(db, kiota, tracker, TimeProvider.System);
    }

    /// <summary>Opens and immediately closes a successful ingest run, returning the run id.</summary>
    private async Task<long> SeedSuccessRunAsync(MysticForgeDbContext db)
    {
        var tracker = new SpellbookIngestRunTracker(db, TimeProvider.System);
        var opened  = await tracker.OpenRunAsync(CancellationToken.None);
        await tracker.CloseRunAsync(
            opened.RunId, "success",
            new RunCloseCounts(null, null, null, null, null, null, null, null, null, null),
            null, CancellationToken.None);
        return opened.RunId;
    }

    private static string BuildFindMyCombosResponse(
        string identity         = "WUBRG",
        string[]? included      = null,
        string[]? almostIncluded = null)
    {
        included      ??= ["combo-1", "combo-2"];
        almostIncluded ??= ["combo-3"];

        var results = new
        {
            identity                                          = identity,
            included                                          = included.Select(id => new { id }).ToArray(),
            includedByChangingCommanders                      = Array.Empty<object>(),
            almostIncluded                                    = almostIncluded.Select(id => new { id }).ToArray(),
            almostIncludedByAddingColors                      = Array.Empty<object>(),
            almostIncludedByChangingCommanders                = Array.Empty<object>(),
            almostIncludedByAddingColorsAndChangingCommanders = Array.Empty<object>(),
        };

        return JsonSerializer.Serialize(new
        {
            count    = 1,
            next     = (string?)null,
            previous = (string?)null,
            results,
        });
    }

    private void RegisterFindMyCombosOk(string responseBody)
    {
        _wm.Given(Request.Create().WithPath("/find-my-combos").UsingPost())
           .RespondWith(WmResponse.Create().WithStatusCode(200)
               .WithHeader("Content-Type", "application/json")
               .WithBody(responseBody));
    }

    private void Register503()
    {
        _wm.Given(Request.Create().WithPath("/find-my-combos").UsingPost())
           .RespondWith(WmResponse.Create().WithStatusCode(503));
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Throws_when_no_successful_ingest_run_exists()
    {
        // No ingest runs at all.
        await using var db  = _db.NewContext();
        var             sut = CreateSut(db);

        var cardA = Guid.NewGuid();
        var cardB = Guid.NewGuid();

        var act = () => sut.FindAsync([cardA], [cardB], CancellationToken.None);

        await act.Should().ThrowAsync<SpellbookProxyException>()
                 .WithMessage("*no successful Spellbook ingest*");
    }

    [Fact]
    public async Task Cache_hit_on_same_deck_doesnt_call_upstream()
    {
        RegisterFindMyCombosOk(BuildFindMyCombosResponse());

        await using var db  = _db.NewContext();
        await SeedSuccessRunAsync(db);

        var cardA      = Guid.NewGuid();
        var commander  = Guid.NewGuid();

        // Seed cards so name lookup works.
        await SpellbookSeedHelpers.InsertCardAsync(db, "Card Alpha", ct: CancellationToken.None);

        var sut = CreateSut(db);

        // First call — cache miss, upstream receives 1 request.
        var r1 = await sut.FindAsync([cardA], [commander], CancellationToken.None);

        // Second call — cache hit, upstream should NOT be called again.
        var r2 = await sut.FindAsync([cardA], [commander], CancellationToken.None);

        _wm.LogEntries.Should().HaveCount(1, "second call must be served from cache");
        r2.Should().BeEquivalentTo(r1);
    }

    [Fact]
    public async Task Cache_miss_when_no_prior_call()
    {
        RegisterFindMyCombosOk(BuildFindMyCombosResponse());

        await using var db = _db.NewContext();
        await SeedSuccessRunAsync(db);

        var cardA     = Guid.NewGuid();
        var commander = Guid.NewGuid();

        var sut    = CreateSut(db);
        var result = await sut.FindAsync([cardA], [commander], CancellationToken.None);

        _wm.LogEntries.Should().HaveCount(1, "uncached deck must hit upstream");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Cache_invalidation_when_ingest_run_advances()
    {
        RegisterFindMyCombosOk(BuildFindMyCombosResponse());

        await using var db = _db.NewContext();

        // Run 1 — closes as success.
        await SeedSuccessRunAsync(db);

        var cardA     = Guid.NewGuid();
        var commander = Guid.NewGuid();

        var sut = CreateSut(db);

        // Call 1: cache miss, writes entry keyed to run 1.
        await sut.FindAsync([cardA], [commander], CancellationToken.None);
        _wm.LogEntries.Should().HaveCount(1);

        // Run 2 — new success run advances the "latest success" pointer.
        _wm.ResetLogEntries();
        RegisterFindMyCombosOk(BuildFindMyCombosResponse("WUBRG", ["combo-X"]));
        await SeedSuccessRunAsync(db);

        // Call 2 with same deck: cache row exists but has old ingest_run_id → miss.
        var result2 = await sut.FindAsync([cardA], [commander], CancellationToken.None);

        _wm.LogEntries.Should().HaveCount(1, "new ingest run should invalidate the cached entry");
        result2.Included.Should().Contain("combo-X");
    }

    [Fact]
    public async Task Upstream_503_throws_SpellbookProxyException()
    {
        Register503();

        await using var db = _db.NewContext();
        await SeedSuccessRunAsync(db);

        var cardA     = Guid.NewGuid();
        var commander = Guid.NewGuid();

        var sut = CreateSut(db);
        var act = () => sut.FindAsync([cardA], [commander], CancellationToken.None);

        await act.Should().ThrowAsync<SpellbookProxyException>();

        // Cache must NOT be populated.
        var cacheCount = await db.FindMyCombosCache.CountAsync();
        cacheCount.Should().Be(0, "failed upstream call must not populate cache");
    }

    [Fact]
    public async Task Hash_distinguishes_commander_vs_main()
    {
        // Card A in main + Commander C in commanders → different hash than Card C main + Commander A.
        var cardA = Guid.NewGuid();
        var cardC = Guid.NewGuid();

        var hashAcMain    = DeckHashCalculator.Compute([cardA], [cardC]);
        var hashCaMain    = DeckHashCalculator.Compute([cardC], [cardA]);

        hashAcMain.Should().NotEqual(hashCaMain,
            "swapping main/commander roles must produce a different deck hash");
    }
}
