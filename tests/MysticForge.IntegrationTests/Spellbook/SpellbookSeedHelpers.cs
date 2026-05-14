using Microsoft.EntityFrameworkCore;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Spellbook;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.IntegrationTests.Spellbook;

internal static class SpellbookSeedHelpers
{
    public static async Task<(Guid OracleId, string Name)> InsertCardAsync(
        MysticForgeDbContext db, string? name = null, CancellationToken ct = default)
    {
        var oracleId   = Guid.NewGuid();
        var cardName   = name ?? $"Test Card {oracleId:N}";
        var oracleText = "Do a thing.";

        var card = new Card
        {
            OracleId          = oracleId,
            Name              = cardName,
            Layout            = CardLayout.Normal,
            OracleText        = oracleText,
            TypeLine          = "Artifact",
            ManaCost          = "{1}",
            Cmc               = 1m,
            ColorIdentity     = Array.Empty<string>(),
            OracleHash        = OracleHasher.HashSingleFace(oracleText),
            LastOracleChange  = DateTimeOffset.UtcNow,
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync(ct);

        return (oracleId, cardName);
    }

    public static async Task<long> OpenRunAsync(MysticForgeDbContext db, CancellationToken ct = default)
    {
        var run = new SpellbookIngestRun { StartedAt = DateTimeOffset.UtcNow };
        db.SpellbookIngestRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run.RunId;
    }

    public static async Task<Combo> InsertComboAsync(
        MysticForgeDbContext                             db,
        string                                           comboId,
        long                                             runId,
        string                                           identity,
        IReadOnlyList<(Guid OracleId, short Position, bool IsCommander)> cards,
        IReadOnlyList<string>?                           featureNames = null,
        string?                                          description  = null,
        DateTimeOffset?                                  deletedAt    = null,
        CancellationToken                                ct           = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Upsert features (by name) if requested.
        var featureIds = new List<long>();
        if (featureNames is { Count: > 0 })
        {
            foreach (var fname in featureNames)
            {
                var existing = db.Features.Local.FirstOrDefault(f => f.Name == fname)
                    ?? await db.Features.FirstOrDefaultAsync(f => f.Name == fname, ct);

                if (existing is not null)
                {
                    featureIds.Add(existing.Id);
                }
                else
                {
                    // Use a deterministic id derived from the name's hash so repeated
                    // calls in the same test session don't collide.
                    var featureId = (long)Math.Abs(fname.GetHashCode()) + 1_000_000L;

                    // Ensure no clash from a previous test that used the same hash slot.
                    var clashCandidate = await db.Features.FindAsync([featureId], ct);
                    if (clashCandidate is null)
                    {
                        var feature = new Feature
                        {
                            Id            = featureId,
                            Name          = fname,
                            Status        = "ok",
                            LastSeenRunId = runId,
                            CreatedAt     = now,
                            UpdatedAt     = now,
                        };
                        db.Features.Add(feature);
                        await db.SaveChangesAsync(ct);
                        featureIds.Add(featureId);
                    }
                    else
                    {
                        featureIds.Add(clashCandidate.Id);
                    }
                }
            }
        }

        var combo = new Combo
        {
            Id            = comboId,
            Identity      = identity,
            Description   = description,
            Status        = "preview",
            LastSeenRunId = runId,
            DeletedAt     = deletedAt,
            CreatedAt     = now,
            UpdatedAt     = now,
        };
        db.Combos.Add(combo);
        await db.SaveChangesAsync(ct);

        foreach (var (oracleId, position, isCommander) in cards)
        {
            db.ComboCards.Add(new ComboCard
            {
                ComboId         = comboId,
                CardPosition    = position,
                CardName        = $"Card@{position}",
                OracleId        = oracleId,
                MustBeCommander = isCommander,
            });
        }

        foreach (var fid in featureIds)
        {
            db.ComboFeatures.Add(new ComboFeature
            {
                ComboId   = comboId,
                FeatureId = fid,
            });
        }

        await db.SaveChangesAsync(ct);

        return combo;
    }

    /// <summary>Inserts or replaces a Feature row directly (bypasses the writer, for seeding).</summary>
    public static async Task<Feature> UpsertFeatureAsync(
        MysticForgeDbContext db,
        long                 id,
        string               name,
        long                 runId,
        CancellationToken    ct = default)
    {
        var now      = DateTimeOffset.UtcNow;
        var existing = await db.Features.FindAsync([id], ct);
        if (existing is not null)
        {
            existing.Name          = name;
            existing.LastSeenRunId = runId;
            existing.DeletedAt     = null;
            existing.UpdatedAt     = now;
            await db.SaveChangesAsync(ct);
            return existing;
        }

        var feature = new Feature
        {
            Id            = id,
            Name          = name,
            Status        = "ok",
            LastSeenRunId = runId,
            CreatedAt     = now,
            UpdatedAt     = now,
        };
        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);
        return feature;
    }

    /// <summary>Inserts or replaces a Template row directly (bypasses the writer, for seeding).</summary>
    public static async Task<Template> UpsertTemplateAsync(
        MysticForgeDbContext db,
        long                 id,
        string               name,
        long                 runId,
        CancellationToken    ct = default)
    {
        var now      = DateTimeOffset.UtcNow;
        var existing = await db.Templates.FindAsync([id], ct);
        if (existing is not null)
        {
            existing.Name          = name;
            existing.LastSeenRunId = runId;
            existing.DeletedAt     = null;
            existing.UpdatedAt     = now;
            await db.SaveChangesAsync(ct);
            return existing;
        }

        var template = new Template
        {
            Id            = id,
            Name          = name,
            LastSeenRunId = runId,
            CreatedAt     = now,
            UpdatedAt     = now,
        };
        db.Templates.Add(template);
        await db.SaveChangesAsync(ct);
        return template;
    }

    /// <summary>Inserts a FindMyCombosCacheEntry directly (bypasses the writer, for seeding).</summary>
    public static async Task InsertCacheEntryAsync(
        MysticForgeDbContext db,
        byte[]               deckHash,
        long                 ingestRunId,
        CancellationToken    ct = default)
    {
        db.FindMyCombosCache.Add(new FindMyCombosCacheEntry
        {
            DeckHash     = deckHash,
            ResponseJson = "{}",
            IngestRunId  = ingestRunId,
            ComputedAt   = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
