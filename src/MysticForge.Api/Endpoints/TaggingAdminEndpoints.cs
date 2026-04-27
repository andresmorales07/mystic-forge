using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Api.Endpoints;

public static class TaggingAdminEndpoints
{
    public static IEndpointRouteBuilder MapTaggingAdminEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/admin/retag/model-bump", async (
            int? limit,
            MysticForgeDbContext db,
            IOpenRouterTaggingClient llm,
            CancellationToken ct) =>
        {
            var current = llm.CurrentModelVersion;
            var actualLimit = limit is > 0 ? limit.Value : 1_000_000;

            const string sql = """
                INSERT INTO card_oracle_events (oracle_id, event_type, new_hash, observed_at)
                SELECT c.oracle_id, 'model_bump', c.oracle_hash, now()
                  FROM cards c
                  LEFT JOIN card_roles cr ON cr.oracle_id = c.oracle_id
                 WHERE cr.model_version IS NULL OR cr.model_version <> {0}
                 GROUP BY c.oracle_id, c.oracle_hash
                 LIMIT {1};
                """;
            var emitted = await db.Database.ExecuteSqlRawAsync(sql, [current, actualLimit], ct);
            return Results.Ok(new { eventsEmitted = emitted, modelVersion = current });
        });

        builder.MapPost("/admin/retag/taxonomy-bump", async (
            string? subtree,
            MysticForgeDbContext db,
            ITaxonomyCache cache,
            CancellationToken ct) =>
        {
            var current = cache.CurrentTaxonomyVersion;

            string sql;
            object[] args;
            if (string.IsNullOrEmpty(subtree))
            {
                sql = """
                    INSERT INTO card_oracle_events (oracle_id, event_type, new_hash, observed_at)
                    SELECT c.oracle_id, 'taxonomy_bump', c.oracle_hash, now()
                      FROM cards c
                      LEFT JOIN card_synergy_hooks csh ON csh.oracle_id = c.oracle_id
                     WHERE csh.taxonomy_version IS NULL OR csh.taxonomy_version <> {0}
                     GROUP BY c.oracle_id, c.oracle_hash;
                    """;
                args = [current];
            }
            else
            {
                sql = """
                    INSERT INTO card_oracle_events (oracle_id, event_type, new_hash, observed_at)
                    SELECT c.oracle_id, 'taxonomy_bump', c.oracle_hash, now()
                      FROM cards c
                      JOIN card_synergy_hook_ancestors a ON a.oracle_id = c.oracle_id
                      JOIN synergy_hooks h ON h.id = a.ancestor_hook_id
                     WHERE h.path = {0}
                     GROUP BY c.oracle_id, c.oracle_hash;
                    """;
                args = [subtree];
            }

            var emitted = await db.Database.ExecuteSqlRawAsync(sql, args, ct);
            return Results.Ok(new { eventsEmitted = emitted, taxonomyVersion = current, subtree });
        });

        return builder;
    }
}
