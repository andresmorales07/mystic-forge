using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;

namespace MysticForge.Infrastructure.Persistence;

public sealed class OutboxClaimer : IOutboxClaimer
{
    private readonly IDbContextFactory<MysticForgeDbContext> _factory;

    public OutboxClaimer(IDbContextFactory<MysticForgeDbContext> factory) { _factory = factory; }

    public async Task<IReadOnlyList<ClaimedEvent>> ClaimBatchAsync(string instanceId, int batchSize, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        // Atomic claim: inner SELECT FOR UPDATE SKIP LOCKED + outer UPDATE RETURNING.
        // claim_attempts <= 5 (not <) so a stranded event whose process crashed mid-attempt
        // gets exactly one self-healing reclaim before falling out.
        const string sql = """
            UPDATE card_oracle_events
               SET claimed_at = now(),
                   claimed_by = {0},
                   claim_attempts = claim_attempts + 1
             WHERE event_id IN (
                 SELECT event_id FROM card_oracle_events
                  WHERE consumed_at IS NULL
                    AND (claimed_at IS NULL OR claimed_at < now() - interval '10 minutes')
                    AND claim_attempts <= 5
                  ORDER BY event_id
                  FOR UPDATE SKIP LOCKED
                  LIMIT {1}
             )
            RETURNING event_id, oracle_id, event_type, claim_attempts;
            """;

        var rows = await db.Database
            .SqlQueryRaw<ClaimRow>(sql, instanceId, batchSize)
            .ToListAsync(ct);

        return rows.Select(r => new ClaimedEvent(
            EventId: r.event_id,
            OracleId: r.oracle_id,
            EventType: r.event_type,
            ClaimAttempts: r.claim_attempts)).ToList();
    }

    // Matches the snake_case columns returned by SqlQueryRaw.
    private sealed record ClaimRow(long event_id, Guid oracle_id, string event_type, short claim_attempts);
}
