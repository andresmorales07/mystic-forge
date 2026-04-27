namespace MysticForge.Domain.Tags;

public sealed class SynergyHook
{
    public long Id { get; init; }
    public required string Path { get; init; }            // 'graveyard_value/reanimate'
    public required string Name { get; init; }            // 'reanimate'
    public long? ParentId { get; init; }
    public required short Depth { get; init; }            // 1 = root, 2 = mid, 3 = leaf
    public required string Description { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
