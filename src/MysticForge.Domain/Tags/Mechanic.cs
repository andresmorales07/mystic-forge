namespace MysticForge.Domain.Tags;

public sealed class Mechanic
{
    public long Id { get; init; }
    public required string Name { get; init; }            // normalized: lowercase + underscores
    public string? DisplayName { get; init; }             // original casing for review UI
    public bool Reviewed { get; init; }
    public required DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
}
