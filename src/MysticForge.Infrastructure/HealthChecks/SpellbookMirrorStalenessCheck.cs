using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MysticForge.Application.Spellbook;
using MysticForge.Infrastructure.Spellbook;

namespace MysticForge.Infrastructure.HealthChecks;

public sealed class SpellbookMirrorStalenessCheck : IHealthCheck
{
    private readonly ISpellbookIngestRunTracker          _runs;
    private readonly IOptions<CommanderSpellbookOptions> _opts;
    private readonly TimeProvider                        _clock;

    public SpellbookMirrorStalenessCheck(
        ISpellbookIngestRunTracker          runs,
        IOptions<CommanderSpellbookOptions> opts,
        TimeProvider                        clock)
    {
        _runs = runs; _opts = opts; _clock = clock;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var latest = await _runs.GetLatestSuccessCompletedAtAsync(ct);
        if (latest is null)
            return HealthCheckResult.Unhealthy("Spellbook mirror has never refreshed successfully");

        var age = _clock.GetUtcNow() - latest.Value;
        var max = TimeSpan.FromHours(_opts.Value.MaxStalenessHours);
        if (age > max)
            return HealthCheckResult.Unhealthy(
                $"Spellbook mirror is stale: last successful refresh {age.TotalHours:F1}h ago (threshold {max.TotalHours}h)");

        return HealthCheckResult.Healthy($"Spellbook mirror refreshed {age.TotalHours:F1}h ago");
    }
}
