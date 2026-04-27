namespace MysticForge.Application.Tagging;

public interface ITagWriter
{
    /// <summary>
    /// In a single transaction:
    ///   1. DELETE all existing tag rows for the oracle_id (across 5 link tables).
    ///   2. INSERT the new tag rows.
    ///   3. UPDATE the event's consumed_at = now().
    /// </summary>
    Task WriteAsync(ClaimedEvent evt, ResolvedTagSet tags, CancellationToken ct);
}
