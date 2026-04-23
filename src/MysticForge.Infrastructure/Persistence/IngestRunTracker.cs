using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Scryfall;
using MysticForge.Infrastructure.Persistence.Entities;

namespace MysticForge.Infrastructure.Persistence;

public sealed class IngestRunTracker : IIngestRunTracker
{
    private readonly MysticForgeDbContext _db;
    private readonly IClock _clock;

    public IngestRunTracker(MysticForgeDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public Task<DateTimeOffset?> GetLastSuccessfulUpdatedAtAsync(string bulkType, CancellationToken ct)
        => _db.ScryfallIngestRuns
            .Where(r => r.BulkType == bulkType && (r.Outcome == "success" || r.Outcome == "skipped"))
            .OrderByDescending(r => r.RunId)
            .Select(r => (DateTimeOffset?)r.ScryfallUpdatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<long> StartAsync(string bulkType, DateTimeOffset scryfallUpdatedAt, CancellationToken ct)
    {
        var run = new ScryfallIngestRun
        {
            BulkType = bulkType,
            ScryfallUpdatedAt = scryfallUpdatedAt,
            StartedAt = _clock.UtcNow,
        };
        _db.ScryfallIngestRuns.Add(run);
        await _db.SaveChangesAsync(ct);
        return run.RunId;
    }

    public async Task CompleteAsync(long runId, string outcome, IngestCounts counts, CancellationToken ct)
    {
        var run = await _db.ScryfallIngestRuns.FindAsync([runId], ct)
            ?? throw new InvalidOperationException($"Ingest run {runId} not found.");
        run.CompletedAt = _clock.UtcNow;
        run.Outcome = outcome;
        run.CardsInserted = counts.CardsInserted;
        run.CardsUpdated = counts.CardsUpdated;
        run.PrintingsInserted = counts.PrintingsInserted;
        run.PrintingsUpdated = counts.PrintingsUpdated;
        run.ErrataEmitted = counts.ErrataEmitted;
        await _db.SaveChangesAsync(ct);
    }

    public async Task FailAsync(long runId, string errorMessage, CancellationToken ct)
    {
        var run = await _db.ScryfallIngestRuns.FindAsync([runId], ct)
            ?? throw new InvalidOperationException($"Ingest run {runId} not found.");
        run.CompletedAt = _clock.UtcNow;
        run.Outcome = "failed";
        run.ErrorMessage = errorMessage;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordSkipAsync(string bulkType, DateTimeOffset scryfallUpdatedAt, CancellationToken ct)
    {
        var run = new ScryfallIngestRun
        {
            BulkType = bulkType,
            ScryfallUpdatedAt = scryfallUpdatedAt,
            StartedAt = _clock.UtcNow,
            CompletedAt = _clock.UtcNow,
            Outcome = "skipped",
        };
        _db.ScryfallIngestRuns.Add(run);
        await _db.SaveChangesAsync(ct);
    }
}
