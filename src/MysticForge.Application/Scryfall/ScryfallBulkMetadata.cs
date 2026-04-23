namespace MysticForge.Application.Scryfall;

public sealed record ScryfallBulkMetadata(
    string Type,
    Uri DownloadUri,
    DateTimeOffset UpdatedAt);
