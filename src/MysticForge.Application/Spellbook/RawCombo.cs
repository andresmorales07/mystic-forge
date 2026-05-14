namespace MysticForge.Application.Spellbook;

public sealed record RawCombo(
    string                             Id,
    string                             Identity,
    string?                            ManaNeeded,
    decimal?                           ManaValueNeeded,
    string?                            OtherPrerequisites,
    string?                            Description,
    string?                            Notes,
    string                             Status,
    bool                               Spoiler,
    string?                            LegalitiesJson,
    string?                            BracketTag,
    int?                               Popularity,
    IReadOnlyList<RawComboCard>        Cards,
    IReadOnlyList<long>                FeatureIds,
    IReadOnlyList<RawComboTemplateRef> TemplateRefs);

public sealed record RawComboCard(
    short   Position,
    string  CardName,
    short   Quantity,
    bool    MustBeCommander,
    string? ZoneLocations);

public sealed record RawComboTemplateRef(long TemplateId, short Quantity);
