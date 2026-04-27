namespace MysticForge.Api.Options;

/// <summary>
/// Mirrors MysticForge.Application.Tagging.TagDrainOptions. Bound at API startup.
/// Kept here to match the existing pattern (ScryfallOptions, OpenRouterOptions).
/// </summary>
public sealed class TaggingOptions
{
    public const string SectionName = "Tagging";

    public int BatchSize { get; init; } = 100;
    public int MaxConcurrency { get; init; } = 10;
    public TimeSpan DrainInterval { get; init; } = TimeSpan.FromSeconds(30);
    public int ClaimExpirySeconds { get; init; } = 600;
    public int MaxClaimAttempts { get; init; } = 5;
    public int RequestTimeoutSeconds { get; init; } = 60;
}
