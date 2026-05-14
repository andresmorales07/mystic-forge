namespace MysticForge.Infrastructure.Spellbook;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Kiota.Abstractions;
using MysticForge.Application.Spellbook;
using MysticForge.Domain.Spellbook;
using MysticForge.Infrastructure.Persistence;
using KiotaClient = MysticForge.CommanderSpellbook.Generated.SpellbookApiClient;
using KiotaModels = MysticForge.CommanderSpellbook.Generated.Models;

public sealed class SpellbookFindMyCombosClient : IFindMyCombosClient
{
    private readonly MysticForgeDbContext       _db;
    private readonly KiotaClient                _kiota;
    private readonly ISpellbookIngestRunTracker _runs;
    private readonly TimeProvider               _clock;

    public SpellbookFindMyCombosClient(
        MysticForgeDbContext db, KiotaClient kiota,
        ISpellbookIngestRunTracker runs, TimeProvider clock)
    {
        _db    = db;
        _kiota = kiota;
        _runs  = runs;
        _clock = clock;
    }

    public async Task<FindMyCombosResult> FindAsync(
        IReadOnlyList<Guid> mainOracleIds,
        IReadOnlyList<Guid> commanderOracleIds,
        CancellationToken   ct)
    {
        var deckHash    = DeckHashCalculator.Compute(mainOracleIds, commanderOracleIds);
        var latestRunId = await _runs.GetLatestSuccessRunIdAsync(ct)
            ?? throw new SpellbookProxyException(
                "Cannot answer find-my-combos: no successful Spellbook ingest has completed yet");

        // Cache lookup: both deck_hash AND ingest_run_id must match.
        var cached = await _db.FindMyCombosCache
            .FirstOrDefaultAsync(c => c.DeckHash == deckHash && c.IngestRunId == latestRunId, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<FindMyCombosResult>(cached.ResponseJson)
                ?? throw new InvalidOperationException("cached find-my-combos response was unreadable");

        // Cache miss: resolve oracle_ids → names from the cards table, build payload, call Spellbook.
        var oracleIds = mainOracleIds.Concat(commanderOracleIds).Distinct().ToList();
        var nameMap   = await _db.Cards
            .Where(c => oracleIds.Contains(c.OracleId))
            .ToDictionaryAsync(c => c.OracleId, c => c.Name, ct);

        var body = new KiotaModels.DeckRequest
        {
            Main       = mainOracleIds
                             .Where(nameMap.ContainsKey)
                             .Select(id => new KiotaModels.CardInDeckRequest { Card = nameMap[id], Quantity = 1 })
                             .ToList(),
            Commanders = commanderOracleIds
                             .Where(nameMap.ContainsKey)
                             .Select(id => new KiotaModels.CardInDeckRequest { Card = nameMap[id], Quantity = 1 })
                             .ToList(),
        };

        KiotaModels.PaginatedFindMyCombosResponseList? upstream;
        try
        {
            upstream = await _kiota.FindMyCombos.PostAsync(body, cancellationToken: ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
        {
            throw new SpellbookProxyException("find-my-combos upstream call failed", ex);
        }

        var result = MapToApplicationDto(upstream);
        var json   = JsonSerializer.Serialize(result);

        // Upsert cache row — ON CONFLICT handles concurrent misses (last writer wins, same result).
        await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO find_my_combos_cache (deck_hash, response, ingest_run_id, computed_at)
            VALUES ({deckHash}, {json}::jsonb, {latestRunId}, now())
            ON CONFLICT (deck_hash) DO UPDATE
              SET response      = EXCLUDED.response,
                  ingest_run_id = EXCLUDED.ingest_run_id,
                  computed_at   = EXCLUDED.computed_at",
            ct);

        return result;
    }

    // -------------------------------------------------------------------------
    // Mapping
    // -------------------------------------------------------------------------

    // PaginatedFindMyCombosResponseList.Results is a single object (not a list).
    // All 7 buckets and Identity are confirmed string-typed on the generated model.
    private static FindMyCombosResult MapToApplicationDto(
        KiotaModels.PaginatedFindMyCombosResponseList? upstream)
    {
        var r = upstream?.Results;
        return new FindMyCombosResult(
            Identity:                                          r?.Identity ?? string.Empty,
            Included:                                          ExtractIds(r?.Included),
            IncludedByChangingCommanders:                      ExtractIds(r?.IncludedByChangingCommanders),
            AlmostIncluded:                                    ExtractIds(r?.AlmostIncluded),
            AlmostIncludedByAddingColors:                      ExtractIds(r?.AlmostIncludedByAddingColors),
            AlmostIncludedByChangingCommanders:                ExtractIds(r?.AlmostIncludedByChangingCommanders),
            AlmostIncludedByAddingColorsAndChangingCommanders: ExtractIds(r?.AlmostIncludedByAddingColorsAndChangingCommanders));
    }

    private static IReadOnlyList<string> ExtractIds(IList<KiotaModels.Variant>? items) =>
        items?.Select(v => v.Id ?? string.Empty).Where(id => id.Length > 0).ToList()
        ?? new List<string>();
}
