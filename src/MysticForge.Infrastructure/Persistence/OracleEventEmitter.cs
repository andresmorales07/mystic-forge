using MysticForge.Application.Scryfall;
using MysticForge.Domain.Events;

namespace MysticForge.Infrastructure.Persistence;

public sealed class OracleEventEmitter : IOracleEventEmitter
{
    private readonly MysticForgeDbContext _db;

    public OracleEventEmitter(MysticForgeDbContext db)
    {
        _db = db;
    }

    public async Task<int> EmitAsync(IReadOnlyList<CardOracleEvent> events, CancellationToken ct)
    {
        if (events.Count == 0) return 0;

        // Clear residue from prior batches in this scope — identity-map conflicts otherwise arise
        // across FlushBatch calls within a single Hangfire job.
        _db.ChangeTracker.Clear();

        await _db.CardOracleEvents.AddRangeAsync(events, ct);
        await _db.SaveChangesAsync(ct);
        return events.Count;
    }
}
