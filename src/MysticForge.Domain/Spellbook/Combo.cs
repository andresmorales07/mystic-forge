namespace MysticForge.Domain.Spellbook;

public sealed class Combo
{
    public required string         Id                  { get; init; }   // Spellbook variant id (UUID string)
    public required string         Identity            { get; set; }    // '', 'w', 'wubrg'
    public          string?        ManaNeeded          { get; set; }
    public          decimal?       ManaValueNeeded     { get; set; }
    public          string?        OtherPrerequisites  { get; set; }
    public          string?        Description         { get; set; }
    public          string?        Notes               { get; set; }
    public required string         Status              { get; set; }
    public          bool           Spoiler             { get; set; }
    public          string?        LegalitiesJson      { get; set; }    // JSONB string
    public          string?        BracketTag          { get; set; }
    public          int?           Popularity          { get; set; }

    public required long           LastSeenRunId       { get; set; }
    public          DateTimeOffset? DeletedAt          { get; set; }

    public required DateTimeOffset CreatedAt           { get; set; }
    public required DateTimeOffset UpdatedAt           { get; set; }

    // Navigations
    public ICollection<ComboCard>     Cards     { get; init; } = new List<ComboCard>();
    public ICollection<ComboFeature>  Features  { get; init; } = new List<ComboFeature>();
    public ICollection<ComboTemplate> Templates { get; init; } = new List<ComboTemplate>();
}
