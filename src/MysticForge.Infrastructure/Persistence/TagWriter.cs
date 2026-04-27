using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;

namespace MysticForge.Infrastructure.Persistence;

public sealed class TagWriter : ITagWriter
{
    private readonly Func<MysticForgeDbContext> _newContext;

    public TagWriter(Func<MysticForgeDbContext> newContext) { _newContext = newContext; }

    public async Task WriteAsync(ClaimedEvent evt, ResolvedTagSet tags, CancellationToken ct)
    {
        await using var db = _newContext();
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM card_roles WHERE oracle_id = {evt.OracleId}", ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM card_synergy_hooks WHERE oracle_id = {evt.OracleId}", ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM card_synergy_hook_ancestors WHERE oracle_id = {evt.OracleId}", ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM card_mechanics WHERE oracle_id = {evt.OracleId}", ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM card_tribal_interest WHERE oracle_id = {evt.OracleId}", ct);

        if (tags.RoleRows.Count > 0)      await db.CardRoles.AddRangeAsync(tags.RoleRows, ct);
        if (tags.HookRows.Count > 0)      await db.CardSynergyHooks.AddRangeAsync(tags.HookRows, ct);
        if (tags.AncestorRows.Count > 0)  await db.CardSynergyHookAncestors.AddRangeAsync(tags.AncestorRows, ct);
        if (tags.MechanicRows.Count > 0)  await db.CardMechanics.AddRangeAsync(tags.MechanicRows, ct);
        if (tags.TribalRows.Count > 0)    await db.CardTribalInterest.AddRangeAsync(tags.TribalRows, ct);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE card_oracle_events SET consumed_at = now() WHERE event_id = {evt.EventId}", ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
