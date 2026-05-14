using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Spellbook;
using MysticForge.Domain.Spellbook;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure.Spellbook;

public sealed class ComboMirrorWriter : IComboMirrorWriter
{
    private readonly MysticForgeDbContext _db;
    private readonly ICardNameResolver    _resolver;
    private readonly TimeProvider         _clock;

    public ComboMirrorWriter(MysticForgeDbContext db, ICardNameResolver resolver, TimeProvider clock)
    {
        _db = db; _resolver = resolver; _clock = clock;
    }

    public async Task<WrittenComboCounts> UpsertFeaturesAsync(
        IReadOnlyList<RawFeature> page, long runId, CancellationToken ct)
    {
        if (page.Count == 0) return new(0, 0);

        var ids      = page.Select(p => p.Id).ToArray();
        var existing = await _db.Features.Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, ct);
        var now      = _clock.GetUtcNow();
        var inserted = 0;
        var updated  = 0;

        foreach (var raw in page)
        {
            if (existing.TryGetValue(raw.Id, out var row))
            {
                row.Name          = raw.Name;
                row.Status        = raw.Status;
                row.Uncountable   = raw.Uncountable;
                row.LastSeenRunId = runId;
                row.DeletedAt     = null;
                row.UpdatedAt     = now;
                updated++;
            }
            else
            {
                _db.Features.Add(new Feature
                {
                    Id            = raw.Id,
                    Name          = raw.Name,
                    Status        = raw.Status,
                    Uncountable   = raw.Uncountable,
                    LastSeenRunId = runId,
                    CreatedAt     = now,
                    UpdatedAt     = now
                });
                inserted++;
            }
        }
        await _db.SaveChangesAsync(ct);
        return new(inserted, updated);
    }

    public async Task<WrittenComboCounts> UpsertTemplatesAsync(
        IReadOnlyList<RawTemplate> page, long runId, CancellationToken ct)
    {
        if (page.Count == 0) return new(0, 0);

        var ids      = page.Select(p => p.Id).ToArray();
        var existing = await _db.Templates.Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);
        var now      = _clock.GetUtcNow();
        var inserted = 0;
        var updated  = 0;

        foreach (var raw in page)
        {
            if (existing.TryGetValue(raw.Id, out var row))
            {
                row.Name          = raw.Name;
                row.ScryfallQuery = raw.ScryfallQuery;
                row.ScryfallApi   = raw.ScryfallApi;
                row.LastSeenRunId = runId;
                row.DeletedAt     = null;
                row.UpdatedAt     = now;
                updated++;
            }
            else
            {
                _db.Templates.Add(new Template
                {
                    Id            = raw.Id,
                    Name          = raw.Name,
                    ScryfallQuery = raw.ScryfallQuery,
                    ScryfallApi   = raw.ScryfallApi,
                    LastSeenRunId = runId,
                    CreatedAt     = now,
                    UpdatedAt     = now
                });
                inserted++;
            }
        }
        await _db.SaveChangesAsync(ct);
        return new(inserted, updated);
    }

    public async Task<WrittenComboCounts> UpsertVariantsAsync(
        IReadOnlyList<RawCombo> page, long runId, CancellationToken ct)
    {
        if (page.Count == 0) return new(0, 0);

        var now      = _clock.GetUtcNow();
        var ids      = page.Select(p => p.Id).ToArray();
        var existing = await _db.Combos
            .Include(c => c.Cards)
            .Include(c => c.Features)
            .Include(c => c.Templates)
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        // Pre-resolve all card names in this page in one batch.
        var allNames = page.SelectMany(p => p.Cards.Select(cc => cc.CardName)).Distinct().ToList();
        var nameMap  = await _resolver.ResolveManyAsync(allNames, ct);

        var inserted = 0;
        var updated  = 0;

        foreach (var raw in page)
        {
            if (existing.TryGetValue(raw.Id, out var row))
            {
                row.Identity           = raw.Identity;
                row.ManaNeeded         = raw.ManaNeeded;
                row.ManaValueNeeded    = raw.ManaValueNeeded;
                row.OtherPrerequisites = raw.OtherPrerequisites;
                row.Description        = raw.Description;
                row.Notes              = raw.Notes;
                row.Status             = raw.Status;
                row.Spoiler            = raw.Spoiler;
                row.LegalitiesJson     = raw.LegalitiesJson;
                row.BracketTag         = raw.BracketTag;
                row.Popularity         = raw.Popularity;
                row.LastSeenRunId      = runId;
                row.DeletedAt          = null;
                row.UpdatedAt          = now;

                _db.ComboCards    .RemoveRange(row.Cards);
                _db.ComboFeatures .RemoveRange(row.Features);
                _db.ComboTemplates.RemoveRange(row.Templates);
                row.Cards    .Clear();
                row.Features .Clear();
                row.Templates.Clear();

                AddChildren(row, raw, nameMap);
                updated++;
            }
            else
            {
                var fresh = new Combo
                {
                    Id                 = raw.Id,
                    Identity           = raw.Identity,
                    ManaNeeded         = raw.ManaNeeded,
                    ManaValueNeeded    = raw.ManaValueNeeded,
                    OtherPrerequisites = raw.OtherPrerequisites,
                    Description        = raw.Description,
                    Notes              = raw.Notes,
                    Status             = raw.Status,
                    Spoiler            = raw.Spoiler,
                    LegalitiesJson     = raw.LegalitiesJson,
                    BracketTag         = raw.BracketTag,
                    Popularity         = raw.Popularity,
                    LastSeenRunId      = runId,
                    CreatedAt          = now,
                    UpdatedAt          = now
                };
                _db.Combos.Add(fresh);
                AddChildren(fresh, raw, nameMap);
                inserted++;
            }
        }
        await _db.SaveChangesAsync(ct);
        return new(inserted, updated);
    }

    private static void AddChildren(
        Combo                              row,
        RawCombo                           raw,
        IReadOnlyDictionary<string, Guid>  nameMap)
    {
        foreach (var c in raw.Cards)
        {
            row.Cards.Add(new ComboCard
            {
                ComboId         = row.Id,
                CardPosition    = c.Position,
                CardName        = c.CardName,
                OracleId        = nameMap.TryGetValue(c.CardName, out var oid) ? oid : null,
                Quantity        = c.Quantity,
                MustBeCommander = c.MustBeCommander,
                ZoneLocations   = c.ZoneLocations
            });
        }
        foreach (var fid in raw.FeatureIds.Distinct())
            row.Features.Add(new ComboFeature { ComboId = row.Id, FeatureId = fid });
        foreach (var tref in raw.TemplateRefs)
            row.Templates.Add(new ComboTemplate { ComboId = row.Id, TemplateId = tref.TemplateId, Quantity = tref.Quantity });
    }

    public async Task<int> SoftMarkUnseenAsync(long runId, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        var combos = await _db.Combos
            .Where(c => c.LastSeenRunId < runId && c.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAt, now), ct);
        var features = await _db.Features
            .Where(f => f.LastSeenRunId < runId && f.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.DeletedAt, now), ct);
        var templates = await _db.Templates
            .Where(t => t.LastSeenRunId < runId && t.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.DeletedAt, now), ct);
        return combos + features + templates;
    }

    public async Task PurgeStaleCacheAsync(long runId, CancellationToken ct)
    {
        await _db.FindMyCombosCache
            .Where(c => c.IngestRunId < runId)
            .ExecuteDeleteAsync(ct);
    }
}
