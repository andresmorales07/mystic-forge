using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Seeding;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Tagging;

[Collection("postgres")]
public sealed class TaxonomySeederTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public TaxonomySeederTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        // Truncate taxonomy tables so each test starts from a clean slate.
        // The shared Postgres container persists data across tests in this collection.
        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE synergy_hooks, taxonomy_metadata RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string LoadYaml() =>
        File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "MysticForge.Infrastructure", "Seeding", "taxonomy-v1.yaml"));

    [Fact]
    public async Task FirstRun_PopulatesAllHooks_AndWritesMetadata()
    {
        await using var ctx = _db.NewContext();
        var seeder = new TaxonomySeeder(ctx, new TaxonomyV1YamlParser(), LoadYaml());

        var result = await seeder.SeedAsync(default);

        result.Inserted.Should().BeGreaterThan(100);
        result.Updated.Should().Be(0);
        result.TaxonomyVersion.Should().NotBeNullOrEmpty();

        await using var verify = _db.NewContext();
        (await verify.SynergyHooks.CountAsync()).Should().Be(result.Inserted);
        (await verify.TaxonomyMetadata.CountAsync()).Should().Be(1);
        var meta = await verify.TaxonomyMetadata.SingleAsync();
        meta.TaxonomyVersion.Should().Be(result.TaxonomyVersion);
    }

    [Fact]
    public async Task SecondRun_IsIdempotent()
    {
        var yaml = LoadYaml();

        await using var ctx1 = _db.NewContext();
        await new TaxonomySeeder(ctx1, new TaxonomyV1YamlParser(), yaml).SeedAsync(default);

        await using var ctx2 = _db.NewContext();
        var second = await new TaxonomySeeder(ctx2, new TaxonomyV1YamlParser(), yaml).SeedAsync(default);

        second.Inserted.Should().Be(0);
        // Updated may be > 0 because we write updated_at = now() on every upsert.

        await using var verify = _db.NewContext();
        (await verify.TaxonomyMetadata.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ChildHooks_HaveCorrectParentIds()
    {
        await using var ctx = _db.NewContext();
        await new TaxonomySeeder(ctx, new TaxonomyV1YamlParser(), LoadYaml()).SeedAsync(default);

        await using var verify = _db.NewContext();
        // counter_matters/proliferate_matters is a real 2-level path in taxonomy-v1.yaml.
        var leaf = await verify.SynergyHooks.SingleAsync(h => h.Path == "counter_matters/proliferate_matters");
        var parent = await verify.SynergyHooks.SingleAsync(h => h.Path == "counter_matters");

        leaf.ParentId.Should().Be(parent.Id);
        leaf.Depth.Should().Be(2);
        parent.Depth.Should().Be(1);
        parent.ParentId.Should().BeNull();
    }
}
