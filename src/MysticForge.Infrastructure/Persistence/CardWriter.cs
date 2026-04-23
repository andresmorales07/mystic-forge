using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Scryfall;
using MysticForge.Domain.Cards;

namespace MysticForge.Infrastructure.Persistence;

public sealed class CardWriter : ICardWriter
{
    private readonly MysticForgeDbContext _db;

    public CardWriter(MysticForgeDbContext db)
    {
        _db = db;
    }

    public async Task<CardUpsertResult> UpsertAsync(IReadOnlyList<Card> cards, CancellationToken ct)
    {
        if (cards.Count == 0) return new CardUpsertResult(0, 0, []);

        var incomingIds = cards.Select(c => c.OracleId).ToArray();
        var existing = await _db.Cards
            .Where(c => incomingIds.Contains(c.OracleId))
            .Select(c => new { c.OracleId, c.OracleHash })
            .ToDictionaryAsync(x => x.OracleId, x => x.OracleHash, ct);

        var changes = new List<OracleChange>();
        int inserted = 0, updated = 0;

        foreach (var card in cards)
        {
            if (existing.TryGetValue(card.OracleId, out var previousHash))
            {
                if (!previousHash.SequenceEqual(card.OracleHash))
                {
                    _db.Cards.Update(card);
                    changes.Add(new OracleChange(card.OracleId, previousHash, card.OracleHash, IsNew: false));
                    updated++;
                }
            }
            else
            {
                await _db.Cards.AddAsync(card, ct);
                changes.Add(new OracleChange(card.OracleId, PreviousHash: null, card.OracleHash, IsNew: true));
                inserted++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new CardUpsertResult(inserted, updated, changes);
    }
}
