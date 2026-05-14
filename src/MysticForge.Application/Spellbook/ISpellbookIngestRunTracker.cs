namespace MysticForge.Application.Spellbook;

public sealed record OpenedRun(long RunId, DateTimeOffset StartedAt);

public sealed record RunCloseCounts(
    int? VariantsSeen,
    int? FeaturesSeen,
    int? TemplatesSeen,
    int? CombosInserted,
    int? CombosUpdated,
    int? CombosSoftDeleted,
    int? FeaturesInserted,
    int? FeaturesUpdated,
    int? TemplatesInserted,
    int? TemplatesUpdated);

public interface ISpellbookIngestRunTracker
{
    Task<OpenedRun>       OpenRunAsync                     (CancellationToken ct);
    Task                  CloseRunAsync                    (long runId, string outcome, RunCloseCounts counts, string? error, CancellationToken ct);
    Task<long?>           GetLatestSuccessRunIdAsync       (CancellationToken ct);
    Task<DateTimeOffset?> GetLatestSuccessCompletedAtAsync (CancellationToken ct);
}
