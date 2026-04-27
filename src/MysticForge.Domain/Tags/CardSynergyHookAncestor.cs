namespace MysticForge.Domain.Tags;

public sealed class CardSynergyHookAncestor
{
    public required Guid OracleId { get; init; }
    public required long AncestorHookId { get; init; }
}
