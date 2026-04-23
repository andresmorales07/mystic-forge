namespace MysticForge.Domain.Events;

public sealed class CardOracleEvent
{
    public long EventId { get; init; }
    public required Guid OracleId { get; init; }
    public required string EventType { get; init; }
    public byte[]? PreviousHash { get; init; }
    public required byte[] NewHash { get; init; }
    public required DateTimeOffset ObservedAt { get; init; }
    public DateTimeOffset? ConsumedAt { get; init; }
}
