namespace MysticForge.Application.Spellbook;

public sealed record RawTemplate(long Id, string Name, string? ScryfallQuery, string? ScryfallApi);
