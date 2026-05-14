namespace MysticForge.Infrastructure.Spellbook;

public sealed class CommanderSpellbookOptions
{
    public const string SectionName = "CommanderSpellbook";

    public required string BaseUrl                       { get; init; }
    public          string UserAgent                     { get; init; } = "MysticForge/0.1";
    public          string RefreshCron                   { get; init; } = "0 6 * * *";
    public          int    MaxStalenessHours             { get; init; } = 48;
    public          int    PageSize                      { get; init; } = 100;
    public          int    PerPageDelayMs                { get; init; } = 250;
    public          int    RefreshRequestTimeoutSeconds  { get; init; } = 30;
    public          int    FindMyCombosTimeoutSeconds    { get; init; } = 15;
}
