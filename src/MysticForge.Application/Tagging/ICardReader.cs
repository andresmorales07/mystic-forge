namespace MysticForge.Application.Tagging;

public interface ICardReader
{
    /// <summary>Reads the card by oracle_id and projects to CardForTagging (LLM input shape).</summary>
    Task<CardForTagging?> GetByOracleIdAsync(Guid oracleId, CancellationToken ct);
}
