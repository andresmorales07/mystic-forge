namespace MysticForge.Application.Tagging;

public interface ITaggingFailureLogger
{
    /// <summary>Inserts a row into tag_failures. Does not change event state.</summary>
    Task LogAsync(ClaimedEvent evt, Exception ex, string modelVersion, CancellationToken ct);

    /// <summary>Marks the event consumed_at = now() (used after exhausting claim attempts).</summary>
    Task MarkConsumedAsync(ClaimedEvent evt, CancellationToken ct);
}
