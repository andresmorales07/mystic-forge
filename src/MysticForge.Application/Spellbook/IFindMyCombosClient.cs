namespace MysticForge.Application.Spellbook;

public interface IFindMyCombosClient
{
    Task<FindMyCombosResult> FindAsync(
        IReadOnlyList<Guid> mainOracleIds,
        IReadOnlyList<Guid> commanderOracleIds,
        CancellationToken   ct);
}

public sealed class SpellbookProxyException : Exception
{
    public SpellbookProxyException(string message, Exception? inner = null) : base(message, inner) { }
}
