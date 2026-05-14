using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Spellbook;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Spellbook;

[Collection("postgres")]
public sealed class CardNameResolverTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public CardNameResolverTests(PostgresContainerFixture pg) { _pg = pg; }

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

    private static CardNameResolver NewResolver(MysticForgeDbContext db) => new(db);

    // -------------------------------------------------------------------------

    [Fact]
    public async Task Resolves_exact_match()
    {
        Guid oracleId;

        await using (var db = _db.NewContext())
        {
            (oracleId, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Bloodghast");
        }

        await using var ctx = _db.NewContext();
        var result = await NewResolver(ctx).ResolveAsync("Bloodghast", CancellationToken.None);

        result.Should().Be(oracleId);
    }

    [Fact]
    public async Task Resolves_case_insensitively()
    {
        Guid oracleId;

        await using (var db = _db.NewContext())
        {
            (oracleId, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Bloodghast");
        }

        await using var ctx = _db.NewContext();
        var resolver = NewResolver(ctx);

        var upper = await resolver.ResolveAsync("BLOODGHAST", CancellationToken.None);
        var lower = await resolver.ResolveAsync("bloodghast", CancellationToken.None);

        upper.Should().Be(oracleId);
        lower.Should().Be(oracleId);
    }

    [Fact]
    public async Task Returns_null_on_miss()
    {
        await using var ctx = _db.NewContext();
        var result = await NewResolver(ctx).ResolveAsync("Nonexistent Card", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Batch_resolution_returns_one_entry_per_hit()
    {
        Guid alphaId, betaId;

        await using (var db = _db.NewContext())
        {
            (alphaId, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Alpha");
            (betaId,  _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Beta");
        }

        await using var ctx = _db.NewContext();
        var result = await NewResolver(ctx).ResolveManyAsync(
            ["Alpha", "BETA", "Gamma"], CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainKey("Alpha").WhoseValue.Should().Be(alphaId);
        result.Should().ContainKey("BETA").WhoseValue.Should().Be(betaId);
        result.Should().NotContainKey("Gamma");
    }

    [Fact]
    public async Task Batch_resolution_handles_duplicate_card_names_deterministically()
    {
        // Real Scryfall data has multiple oracle_ids sharing a name (functional reprints,
        // certain tokens). The resolver must not throw on duplicate keys and must pick
        // deterministically (lowest OracleId wins).
        Guid firstId, secondId;

        await using (var db = _db.NewContext())
        {
            (firstId,  _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Starscape Cleric");
            (secondId, _) = await SpellbookSeedHelpers.InsertCardAsync(db, "Starscape Cleric");
        }

        var expected = firstId.CompareTo(secondId) < 0 ? firstId : secondId;

        await using var ctx = _db.NewContext();
        var resolver = NewResolver(ctx);

        var single = await resolver.ResolveAsync("Starscape Cleric", CancellationToken.None);
        var batch  = await resolver.ResolveManyAsync(["Starscape Cleric"], CancellationToken.None);

        single.Should().Be(expected);
        batch.Should().HaveCount(1);
        batch["Starscape Cleric"].Should().Be(expected);
    }
}
