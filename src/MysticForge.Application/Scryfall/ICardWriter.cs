using MysticForge.Domain.Cards;

namespace MysticForge.Application.Scryfall;

public interface ICardWriter
{
    Task<CardUpsertResult> UpsertAsync(IReadOnlyList<Card> cards, CancellationToken ct);
}

public sealed record CardUpsertResult(int Inserted, int Updated, IReadOnlyList<OracleChange> Changes);

public sealed record OracleChange(Guid OracleId, byte[]? PreviousHash, byte[] NewHash, bool IsNew);
