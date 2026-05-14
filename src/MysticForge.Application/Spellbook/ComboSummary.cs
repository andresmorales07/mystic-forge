namespace MysticForge.Application.Spellbook;

public sealed record ComboSummary(
    string                   ComboId,
    string                   Identity,
    string?                  Description,
    bool                     MustBeCommander,
    IReadOnlyList<string>    FeatureNames);
