namespace MysticForge.Domain.Spellbook;

public sealed class Feature
{
    public required long           Id            { get; init; }
    public required string         Name          { get; set; }
    public required string         Status        { get; set; }
    public          bool           Uncountable   { get; set; }

    public required long           LastSeenRunId { get; set; }
    public          DateTimeOffset? DeletedAt    { get; set; }

    public required DateTimeOffset CreatedAt     { get; set; }
    public required DateTimeOffset UpdatedAt     { get; set; }
}
