namespace MysticForge.Domain.Spellbook;

public sealed class Template
{
    public required long           Id             { get; init; }
    public required string         Name           { get; set; }
    public          string?        ScryfallQuery  { get; set; }
    public          string?        ScryfallApi    { get; set; }

    public required long           LastSeenRunId  { get; set; }
    public          DateTimeOffset? DeletedAt     { get; set; }

    public required DateTimeOffset CreatedAt      { get; set; }
    public required DateTimeOffset UpdatedAt      { get; set; }
}
