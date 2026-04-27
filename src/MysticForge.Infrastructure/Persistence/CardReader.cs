using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;

namespace MysticForge.Infrastructure.Persistence;

public sealed class CardReader : ICardReader
{
    private readonly IDbContextFactory<MysticForgeDbContext> _factory;

    public CardReader(IDbContextFactory<MysticForgeDbContext> factory) { _factory = factory; }

    public async Task<CardForTagging?> GetByOracleIdAsync(Guid oracleId, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var card = await db.Cards.AsNoTracking().SingleOrDefaultAsync(c => c.OracleId == oracleId, ct);
        if (card is null) return null;

        var faces = card.Faces?.Select(f => new CardFaceForTagging(
            f.Name, f.ManaCost, f.TypeLine, f.OracleText)).ToList();

        return new CardForTagging(
            Name: card.Name,
            ManaCost: card.ManaCost,
            TypeLine: card.TypeLine,
            OracleText: card.OracleText,
            Faces: faces);
    }
}
