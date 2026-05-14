using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Spellbook;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure.Spellbook;

public sealed class CardNameResolver : ICardNameResolver
{
    private readonly MysticForgeDbContext _db;
    public CardNameResolver(MysticForgeDbContext db) { _db = db; }

    public async Task<Guid?> ResolveAsync(string name, CancellationToken ct)
    {
        var lowered = name.ToLowerInvariant();
        return await _db.Cards
            .Where(c => c.Name.ToLower() == lowered)
            .Select(c => (Guid?)c.OracleId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, Guid>> ResolveManyAsync(
        IReadOnlyList<string> names, CancellationToken ct)
    {
        if (names.Count == 0) return new Dictionary<string, Guid>();

        // Lowered → original mapping to preserve caller casing in returned dict.
        var loweredToOriginal = names.ToDictionary(
            n => n.ToLowerInvariant(), n => n, StringComparer.Ordinal);
        var loweredKeys = loweredToOriginal.Keys.ToArray();

        var rows = await _db.Cards
            .Where(c => loweredKeys.Contains(c.Name.ToLower()))
            .Select(c => new { LoweredName = c.Name.ToLower(), c.OracleId })
            .ToListAsync(ct);

        return rows.ToDictionary(
            r => loweredToOriginal[r.LoweredName], r => r.OracleId, StringComparer.Ordinal);
    }
}
