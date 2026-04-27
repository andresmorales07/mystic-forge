namespace MysticForge.Domain.Tags;

public sealed class CardRole
{
    public required Guid OracleId { get; init; }
    public required string Role { get; init; }
    public required string ModelVersion { get; init; }
    public required string TaxonomyVersion { get; init; }
    public required DateTimeOffset TaggedAt { get; init; }
    public required string Source { get; init; }          // 'llm' or 'human'
}
