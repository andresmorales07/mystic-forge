using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Spellbook;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Spellbook;

[Collection("postgres")]
public sealed class ComboReaderTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public ComboReaderTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE spellbook_ingest_runs RESTART IDENTITY CASCADE");
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE cards RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ComboReader NewReader(MysticForgeDbContext db) => new(db);

    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByOracleId_returns_empty_for_unknown_oracle_id()
    {
        await using var db = _db.NewContext();
        var reader = NewReader(db);

        var result = await reader.GetByOracleIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByOracleId_returns_combos_with_feature_names_for_known_card()
    {
        Guid oracleId;
        const string comboId     = "combo-reader-happy-path";
        const string featureName = "Infinite Mana";

        await using (var db = _db.NewContext())
        {
            (oracleId, _) = await SpellbookSeedHelpers.InsertCardAsync(db);
            var runId     = await SpellbookSeedHelpers.OpenRunAsync(db);
            await SpellbookSeedHelpers.InsertComboAsync(
                db,
                comboId:      comboId,
                runId:        runId,
                identity:     "ug",
                cards:        [(oracleId, 1, true)],
                featureNames: [featureName],
                description:  "Make lots of mana.");
        }

        await using var reader = _db.NewContext();
        var result = await NewReader(reader).GetByOracleIdAsync(oracleId, CancellationToken.None);

        result.Should().HaveCount(1);
        var summary = result[0];
        summary.ComboId.Should().Be(comboId);
        summary.Identity.Should().Be("ug");
        summary.Description.Should().Be("Make lots of mana.");
        summary.MustBeCommander.Should().BeTrue();
        summary.FeatureNames.Should().ContainSingle(n => n == featureName);
    }

    [Fact]
    public async Task GetByOracleId_filters_soft_deleted_combos()
    {
        Guid oracleId;
        const string comboId = "combo-reader-soft-delete";

        await using (var db = _db.NewContext())
        {
            (oracleId, _) = await SpellbookSeedHelpers.InsertCardAsync(db);
            var runId     = await SpellbookSeedHelpers.OpenRunAsync(db);
            await SpellbookSeedHelpers.InsertComboAsync(
                db,
                comboId:   comboId,
                runId:     runId,
                identity:  "w",
                cards:     [(oracleId, 1, false)],
                deletedAt: DateTimeOffset.UtcNow);
        }

        await using var reader = _db.NewContext();
        var result = await NewReader(reader).GetByOracleIdAsync(oracleId, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByOracleIds_batches_lookup_for_multiple_cards()
    {
        Guid oracleId1, oracleId2;
        const string comboId1 = "combo-reader-batch-1";
        const string comboId2 = "combo-reader-batch-2";

        await using (var db = _db.NewContext())
        {
            (oracleId1, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Batch Card A");
            (oracleId2, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Batch Card B");
            var runId      = await SpellbookSeedHelpers.OpenRunAsync(db);

            await SpellbookSeedHelpers.InsertComboAsync(
                db,
                comboId:  comboId1,
                runId:    runId,
                identity: "r",
                cards:    [(oracleId1, 1, false)]);

            await SpellbookSeedHelpers.InsertComboAsync(
                db,
                comboId:  comboId2,
                runId:    runId,
                identity: "b",
                cards:    [(oracleId2, 1, false)]);
        }

        await using var reader = _db.NewContext();
        var result = await NewReader(reader).GetByOracleIdsAsync(
            [oracleId1, oracleId2], CancellationToken.None);

        result.Should().ContainKey(oracleId1);
        result.Should().ContainKey(oracleId2);
        result[oracleId1].Should().ContainSingle(s => s.ComboId == comboId1);
        result[oracleId2].Should().ContainSingle(s => s.ComboId == comboId2);
    }
}
