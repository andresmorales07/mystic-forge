namespace MysticForge.Api.Options;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string ApiKey { get; init; } = string.Empty;
    public required string BaseUrl { get; init; }
    public required string TaggingModel { get; init; }
}
