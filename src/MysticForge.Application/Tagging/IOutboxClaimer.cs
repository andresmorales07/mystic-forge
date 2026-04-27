namespace MysticForge.Application.Tagging;

public interface IOutboxClaimer
{
    /// <summary>
    /// Claims up to <paramref name="batchSize"/> unconsumed events.
    /// Atomically: sets claimed_at = now(), claimed_by = instanceId, claim_attempts++.
    /// Skips events claimed within the last 10 minutes and events with claim_attempts > 5.
    /// </summary>
    Task<IReadOnlyList<ClaimedEvent>> ClaimBatchAsync(string instanceId, int batchSize, CancellationToken ct);
}
