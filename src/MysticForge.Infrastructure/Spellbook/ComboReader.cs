using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Spellbook;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure.Spellbook;

public sealed class ComboReader : IComboReader
{
    private readonly MysticForgeDbContext _db;
    public ComboReader(MysticForgeDbContext db) { _db = db; }

    public async Task<IReadOnlyList<ComboSummary>> GetByOracleIdAsync(Guid oracleId, CancellationToken ct)
    {
        var rows = await _db.ComboCards
            .Where(cc => cc.OracleId == oracleId)
            .Where(cc => cc.Combo!.DeletedAt == null)
            .Select(cc => new
            {
                cc.ComboId,
                cc.Combo!.Identity,
                cc.Combo.Description,
                cc.MustBeCommander,
                FeatureNames = cc.Combo.Features
                    .Where(f => f.Feature!.DeletedAt == null)
                    .Select(f => f.Feature!.Name)
                    .ToList()
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ComboSummary(r.ComboId, r.Identity, r.Description, r.MustBeCommander, r.FeatureNames))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ComboSummary>>>
        GetByOracleIdsAsync(IReadOnlyList<Guid> oracleIds, CancellationToken ct)
    {
        if (oracleIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<ComboSummary>>();

        var rows = await _db.ComboCards
            .Where(cc => cc.OracleId != null && oracleIds.Contains(cc.OracleId.Value))
            .Where(cc => cc.Combo!.DeletedAt == null)
            .Select(cc => new
            {
                OracleId     = cc.OracleId!.Value,
                cc.ComboId,
                cc.Combo!.Identity,
                cc.Combo.Description,
                cc.MustBeCommander,
                FeatureNames = cc.Combo.Features
                    .Where(f => f.Feature!.DeletedAt == null)
                    .Select(f => f.Feature!.Name)
                    .ToList()
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.OracleId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ComboSummary>)g
                    .Select(r => new ComboSummary(r.ComboId, r.Identity, r.Description, r.MustBeCommander, r.FeatureNames))
                    .ToList());
    }
}
