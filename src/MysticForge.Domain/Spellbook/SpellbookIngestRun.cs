namespace MysticForge.Domain.Spellbook;

public sealed class SpellbookIngestRun
{
    public          long            RunId               { get; init; }    // BIGSERIAL
    public required DateTimeOffset  StartedAt           { get; init; }
    public          DateTimeOffset? CompletedAt         { get; set; }
    public          string?         Outcome             { get; set; }    // 'success'|'partial'|'failed'|'skipped'

    public          int?            VariantsSeen        { get; set; }
    public          int?            FeaturesSeen        { get; set; }
    public          int?            TemplatesSeen       { get; set; }

    public          int?            CombosInserted      { get; set; }
    public          int?            CombosUpdated       { get; set; }
    public          int?            CombosSoftDeleted   { get; set; }
    public          int?            FeaturesInserted    { get; set; }
    public          int?            FeaturesUpdated     { get; set; }
    public          int?            TemplatesInserted   { get; set; }
    public          int?            TemplatesUpdated    { get; set; }

    public          string?         ErrorMessage        { get; set; }
}
