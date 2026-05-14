namespace MysticForge.Application.Spellbook;

// Each list is a list of combo (variant) ids; Phase 4 resolves to full ComboSummary via IComboReader.
public sealed record FindMyCombosResult(
    string                Identity,
    IReadOnlyList<string> Included,
    IReadOnlyList<string> IncludedByChangingCommanders,
    IReadOnlyList<string> AlmostIncluded,
    IReadOnlyList<string> AlmostIncludedByAddingColors,
    IReadOnlyList<string> AlmostIncludedByChangingCommanders,
    IReadOnlyList<string> AlmostIncludedByAddingColorsAndChangingCommanders);
