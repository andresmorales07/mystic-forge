namespace MysticForge.Application.Spellbook;

public sealed record WrittenComboCounts(int Inserted, int Updated);

public interface IComboMirrorWriter
{
    Task<WrittenComboCounts> UpsertFeaturesAsync  (IReadOnlyList<RawFeature>  page, long runId, CancellationToken ct);
    Task<WrittenComboCounts> UpsertTemplatesAsync (IReadOnlyList<RawTemplate> page, long runId, CancellationToken ct);
    Task<WrittenComboCounts> UpsertVariantsAsync  (IReadOnlyList<RawCombo>    page, long runId, CancellationToken ct);
    Task<int>                SoftMarkUnseenAsync  (long runId, CancellationToken ct);   // returns total count soft-deleted across all 3 tables
    Task                     PurgeStaleCacheAsync (long runId, CancellationToken ct);
}
