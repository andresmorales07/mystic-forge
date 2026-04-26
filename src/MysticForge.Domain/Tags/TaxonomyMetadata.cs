namespace MysticForge.Domain.Tags;

public sealed class TaxonomyMetadata
{
    public int Id { get; init; }                          // always 1; CHECK constraint enforces
    public required string TaxonomyVersion { get; init; }
    public required DateTimeOffset SeededAt { get; init; }
}
