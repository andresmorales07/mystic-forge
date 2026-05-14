using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Spellbook;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Spellbook;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Spellbook;

[Collection("postgres")]
public sealed class SpellbookIngestRunTrackerTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public SpellbookIngestRunTrackerTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE spellbook_ingest_runs RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ISpellbookIngestRunTracker NewTracker(MysticForgeDbContext db) =>
        new SpellbookIngestRunTracker(db, TimeProvider.System);

    [Fact]
    public async Task OpenRun_writes_a_row_with_started_at_and_returns_run_id()
    {
        await using var db = _db.NewContext();
        var tracker = NewTracker(db);

        var opened = await tracker.OpenRunAsync(CancellationToken.None);

        opened.RunId.Should().BeGreaterThan(0);
        var row = await db.SpellbookIngestRuns.FindAsync(opened.RunId);
        row.Should().NotBeNull();
        row!.StartedAt.Should().BeCloseTo(opened.StartedAt, TimeSpan.FromSeconds(2));
        row.CompletedAt.Should().BeNull();
        row.Outcome.Should().BeNull();
    }

    [Fact]
    public async Task CloseRun_sets_outcome_completed_at_and_counts()
    {
        await using var db = _db.NewContext();
        var tracker = NewTracker(db);
        var opened = await tracker.OpenRunAsync(CancellationToken.None);

        var counts = new RunCloseCounts(
            VariantsSeen:      100, FeaturesSeen:     10, TemplatesSeen:     5,
            CombosInserted:     50, CombosUpdated:    50, CombosSoftDeleted: 0,
            FeaturesInserted:   10, FeaturesUpdated:   0,
            TemplatesInserted:   5, TemplatesUpdated:  0);
        await tracker.CloseRunAsync(opened.RunId, "success", counts, error: null, CancellationToken.None);

        await using var db2 = _db.NewContext();
        var row = await db2.SpellbookIngestRuns.FindAsync(opened.RunId);
        row!.Outcome.Should().Be("success");
        row.CompletedAt.Should().NotBeNull();
        row.VariantsSeen.Should().Be(100);
        row.CombosInserted.Should().Be(50);
    }

    [Fact]
    public async Task GetLatestSuccessRunId_returns_most_recent_success_only()
    {
        await using var db = _db.NewContext();
        var tracker = NewTracker(db);

        var r1 = await tracker.OpenRunAsync(default);
        await tracker.CloseRunAsync(r1.RunId, "success", EmptyCounts, null,   default);

        var r2 = await tracker.OpenRunAsync(default);
        await tracker.CloseRunAsync(r2.RunId, "failed",  EmptyCounts, "boom", default);

        var r3 = await tracker.OpenRunAsync(default);
        await tracker.CloseRunAsync(r3.RunId, "partial", EmptyCounts, null,   default);

        var latest = await tracker.GetLatestSuccessRunIdAsync(default);
        latest.Should().Be(r1.RunId);   // r2 failed, r3 partial; only r1 was success
    }

    [Fact]
    public async Task GetLatestSuccessRunId_returns_null_when_no_runs_succeeded()
    {
        await using var db = _db.NewContext();
        var tracker = NewTracker(db);

        var r1 = await tracker.OpenRunAsync(default);
        await tracker.CloseRunAsync(r1.RunId, "failed", EmptyCounts, "boom", default);

        var latest = await tracker.GetLatestSuccessRunIdAsync(default);
        latest.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestSuccessCompletedAt_returns_timestamp_of_most_recent_success()
    {
        await using var db = _db.NewContext();
        var tracker = NewTracker(db);

        var r = await tracker.OpenRunAsync(default);
        await tracker.CloseRunAsync(r.RunId, "success", EmptyCounts, null, default);

        var ts = await tracker.GetLatestSuccessCompletedAtAsync(default);
        ts.Should().NotBeNull();
        ts!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    private static readonly RunCloseCounts EmptyCounts =
        new(null, null, null, null, null, null, null, null, null, null);
}
