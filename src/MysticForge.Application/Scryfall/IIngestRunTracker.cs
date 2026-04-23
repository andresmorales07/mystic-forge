namespace MysticForge.Application.Scryfall;

public interface IIngestRunTracker
{
    Task<DateTimeOffset?> GetLastSuccessfulUpdatedAtAsync(string bulkType, CancellationToken ct);
    Task<long> StartAsync(string bulkType, DateTimeOffset scryfallUpdatedAt, CancellationToken ct);
    Task CompleteAsync(long runId, string outcome, IngestCounts counts, CancellationToken ct);
    Task FailAsync(long runId, string errorMessage, CancellationToken ct);
    Task RecordSkipAsync(string bulkType, DateTimeOffset scryfallUpdatedAt, CancellationToken ct);
}

public sealed record IngestCounts(
    int CardsInserted,
    int CardsUpdated,
    int PrintingsInserted,
    int PrintingsUpdated,
    int ErrataEmitted);
