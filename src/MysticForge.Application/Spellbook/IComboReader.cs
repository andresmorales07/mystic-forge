namespace MysticForge.Application.Spellbook;

public interface IComboReader
{
    Task<IReadOnlyList<ComboSummary>>                            GetByOracleIdAsync (Guid oracleId, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ComboSummary>>> GetByOracleIdsAsync(IReadOnlyList<Guid> oracleIds, CancellationToken ct);
}
