using MysticForge.Domain.Tags;

namespace MysticForge.Application.Tagging;

/// <summary>The tag rows ready to insert for a single card. All produced by TagSetResolver.</summary>
public sealed record ResolvedTagSet(
    IReadOnlyList<CardRole> RoleRows,
    IReadOnlyList<CardSynergyHook> HookRows,
    IReadOnlyList<CardSynergyHookAncestor> AncestorRows,
    IReadOnlyList<CardMechanic> MechanicRows,
    IReadOnlyList<CardTribalInterest> TribalRows);
