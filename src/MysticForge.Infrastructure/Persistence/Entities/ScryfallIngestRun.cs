namespace MysticForge.Infrastructure.Persistence.Entities;

public sealed class ScryfallIngestRun
{
    public long RunId { get; set; }
    public required string BulkType { get; init; }
    public required DateTimeOffset ScryfallUpdatedAt { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Outcome { get; set; }
    public int? CardsInserted { get; set; }
    public int? CardsUpdated { get; set; }
    public int? PrintingsInserted { get; set; }
    public int? PrintingsUpdated { get; set; }
    public int? ErrataEmitted { get; set; }
    public string? ErrorMessage { get; set; }
}
