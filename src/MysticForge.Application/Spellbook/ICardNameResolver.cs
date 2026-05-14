namespace MysticForge.Application.Spellbook;

public interface ICardNameResolver
{
    /// <summary>Returns oracle_id for the given card name if present in <c>cards</c>. Match is case-insensitive
    /// over the full name; multi-face names use the "//" form. Returns null on miss.</summary>
    Task<Guid?>                             ResolveAsync     (string name, CancellationToken ct);
    Task<IReadOnlyDictionary<string, Guid>> ResolveManyAsync (IReadOnlyList<string> names, CancellationToken ct);
}
