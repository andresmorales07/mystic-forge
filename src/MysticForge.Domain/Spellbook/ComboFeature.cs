namespace MysticForge.Domain.Spellbook;

public sealed class ComboFeature
{
    public required string ComboId   { get; init; }
    public required long   FeatureId { get; init; }

    public Combo?   Combo   { get; init; }
    public Feature? Feature { get; init; }
}
