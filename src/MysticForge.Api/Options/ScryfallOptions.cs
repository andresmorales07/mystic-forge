namespace MysticForge.Api.Options;

public sealed class ScryfallOptions
{
    public const string SectionName = "Scryfall";

    public required string BulkDataEndpoint { get; init; }
    public required string ContactEmail { get; init; }
    public required string BulkFileType { get; init; }
}
