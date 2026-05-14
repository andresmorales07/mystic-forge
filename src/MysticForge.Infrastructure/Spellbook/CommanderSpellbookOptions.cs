namespace MysticForge.Infrastructure.Spellbook;

public sealed class CommanderSpellbookOptions
{
    public const string SectionName = "CommanderSpellbook";

    public required string BaseUrl                      { get; init; }
    public int             PageSize                     { get; init; } = 100;
    public int             PerPageDelayMs               { get; init; } = 200;
    public int             RefreshRequestTimeoutSeconds { get; init; } = 30;
    public int             MaxStalenessHours            { get; init; } = 48;
}
