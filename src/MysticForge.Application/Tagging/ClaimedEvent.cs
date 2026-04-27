namespace MysticForge.Application.Tagging;

/// <summary>A row returned from OutboxClaimer.ClaimBatchAsync. Decoupled from EF tracking.</summary>
public sealed record ClaimedEvent(
    long EventId,
    Guid OracleId,
    string EventType,
    short ClaimAttempts);
