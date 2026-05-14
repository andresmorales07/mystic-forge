using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MysticForge.Application.Spellbook;
using MysticForge.Infrastructure.HealthChecks;
using MysticForge.Infrastructure.Spellbook;
using MysticForge.IntegrationTests.Harness;
using Xunit;

namespace MysticForge.IntegrationTests.Spellbook;

[Collection("postgres")]
public sealed class SpellbookMirrorStalenessCheckTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _pg;
    private DatabaseFixture _db = null!;

    public SpellbookMirrorStalenessCheckTests(PostgresContainerFixture pg) { _pg = pg; }

    public async Task InitializeAsync()
    {
        _db = new DatabaseFixture(_pg);
        await _db.InitializeAsync();

        await using var ctx = _db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE spellbook_ingest_runs RESTART IDENTITY CASCADE");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static readonly RunCloseCounts EmptyCounts =
        new(null, null, null, null, null, null, null, null, null, null);

    private static IOptions<CommanderSpellbookOptions> DefaultOptions(int maxStalenessHours = 48) =>
        Options.Create(new CommanderSpellbookOptions
        {
            BaseUrl           = "http://localhost/",
            MaxStalenessHours = maxStalenessHours,
        });

    private static HealthCheckContext MakeContext(IHealthCheck check) =>
        new() { Registration = new HealthCheckRegistration("test", check, null, null) };

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Returns_unhealthy_when_no_successful_run_exists()
    {
        await using var db      = _db.NewContext();
        var             tracker = new SpellbookIngestRunTracker(db, TimeProvider.System);
        var             check   = new SpellbookMirrorStalenessCheck(tracker, DefaultOptions(), TimeProvider.System);

        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("never refreshed");
    }

    [Fact]
    public async Task Returns_healthy_when_latest_success_is_within_threshold()
    {
        await using var db      = _db.NewContext();
        var             tracker = new SpellbookIngestRunTracker(db, TimeProvider.System);

        var opened = await tracker.OpenRunAsync(default);
        await tracker.CloseRunAsync(opened.RunId, "success", EmptyCounts, null, default);

        var check  = new SpellbookMirrorStalenessCheck(tracker, DefaultOptions(maxStalenessHours: 48), TimeProvider.System);
        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Returns_unhealthy_when_latest_success_is_older_than_threshold()
    {
        await using var db      = _db.NewContext();
        var             tracker = new SpellbookIngestRunTracker(db, TimeProvider.System);

        // Open and close a run successfully, then move completed_at 100h into the past.
        var opened = await tracker.OpenRunAsync(default);
        await tracker.CloseRunAsync(opened.RunId, "success", EmptyCounts, null, default);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE spellbook_ingest_runs SET completed_at = now() - interval '100 hours' WHERE run_id = {opened.RunId}");

        // Default threshold is 48 hours; 100h ago is clearly stale.
        var check  = new SpellbookMirrorStalenessCheck(tracker, DefaultOptions(maxStalenessHours: 48), TimeProvider.System);
        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("stale");
    }
}
