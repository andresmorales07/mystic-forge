namespace MysticForge.Domain.Spellbook;

public sealed class FindMyCombosCacheEntry
{
    public required byte[]         DeckHash     { get; init; }     // sha256, 32 bytes
    public required string         ResponseJson { get; set; }      // JSONB
    public required long           IngestRunId  { get; set; }
    public required DateTimeOffset ComputedAt   { get; set; }
}
