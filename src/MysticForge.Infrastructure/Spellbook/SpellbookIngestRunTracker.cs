using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Spellbook;
using MysticForge.Domain.Spellbook;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure.Spellbook;

public sealed class SpellbookIngestRunTracker : ISpellbookIngestRunTracker
{
    private readonly MysticForgeDbContext _db;
    private readonly TimeProvider         _clock;

    public SpellbookIngestRunTracker(MysticForgeDbContext db, TimeProvider clock)
    {
        _db    = db;
        _clock = clock;
    }

    public async Task<OpenedRun> OpenRunAsync(CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var row = new SpellbookIngestRun { StartedAt = now };
        _db.SpellbookIngestRuns.Add(row);
        await _db.SaveChangesAsync(ct);
        return new OpenedRun(row.RunId, now);
    }

    public async Task CloseRunAsync(long runId, string outcome, RunCloseCounts c, string? error, CancellationToken ct)
    {
        var row = await _db.SpellbookIngestRuns.FindAsync(new object?[] { runId }, ct)
            ?? throw new InvalidOperationException($"run_id {runId} not found");

        row.CompletedAt       = _clock.GetUtcNow();
        row.Outcome           = outcome;
        row.ErrorMessage      = error;

        row.VariantsSeen      = c.VariantsSeen;
        row.FeaturesSeen      = c.FeaturesSeen;
        row.TemplatesSeen     = c.TemplatesSeen;
        row.CombosInserted    = c.CombosInserted;
        row.CombosUpdated     = c.CombosUpdated;
        row.CombosSoftDeleted = c.CombosSoftDeleted;
        row.FeaturesInserted  = c.FeaturesInserted;
        row.FeaturesUpdated   = c.FeaturesUpdated;
        row.TemplatesInserted = c.TemplatesInserted;
        row.TemplatesUpdated  = c.TemplatesUpdated;

        await _db.SaveChangesAsync(ct);
    }

    public Task<long?> GetLatestSuccessRunIdAsync(CancellationToken ct) =>
        _db.SpellbookIngestRuns
           .Where(r => r.Outcome == "success")
           .OrderByDescending(r => r.RunId)
           .Select(r => (long?)r.RunId)
           .FirstOrDefaultAsync(ct);

    public Task<DateTimeOffset?> GetLatestSuccessCompletedAtAsync(CancellationToken ct) =>
        _db.SpellbookIngestRuns
           .Where(r => r.Outcome == "success")
           .OrderByDescending(r => r.RunId)
           .Select(r => r.CompletedAt)
           .FirstOrDefaultAsync(ct);
}
