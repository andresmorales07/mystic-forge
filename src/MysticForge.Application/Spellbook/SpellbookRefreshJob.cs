using Hangfire;
using Microsoft.Extensions.Logging;

namespace MysticForge.Application.Spellbook;

[DisableConcurrentExecution(timeoutInSeconds: 1800)]
public sealed class SpellbookRefreshJob
{
    private readonly ISpellbookRefreshClient      _client;
    private readonly IComboMirrorWriter           _writer;
    private readonly ISpellbookIngestRunTracker   _runs;
    private readonly ILogger<SpellbookRefreshJob> _log;

    public SpellbookRefreshJob(
        ISpellbookRefreshClient      client,
        IComboMirrorWriter           writer,
        ISpellbookIngestRunTracker   runs,
        ILogger<SpellbookRefreshJob> log)
    {
        _client = client; _writer = writer; _runs = runs; _log = log;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var opened = await _runs.OpenRunAsync(ct);
        var counts = new MutableCounts();
        try
        {
            var anyFailed = false;
            anyFailed |= !await DrainAsync("features",  opened.RunId, ct, async () =>
            {
                int seen = 0, inserted = 0, updated = 0;
                await foreach (var page in _client.StreamFeaturesAsync(ct))
                {
                    var w = await _writer.UpsertFeaturesAsync(page, opened.RunId, ct);
                    seen += page.Count; inserted += w.Inserted; updated += w.Updated;
                }
                counts.FeaturesSeen     = seen;
                counts.FeaturesInserted = inserted;
                counts.FeaturesUpdated  = updated;
            });
            anyFailed |= !await DrainAsync("templates", opened.RunId, ct, async () =>
            {
                int seen = 0, inserted = 0, updated = 0;
                await foreach (var page in _client.StreamTemplatesAsync(ct))
                {
                    var w = await _writer.UpsertTemplatesAsync(page, opened.RunId, ct);
                    seen += page.Count; inserted += w.Inserted; updated += w.Updated;
                }
                counts.TemplatesSeen     = seen;
                counts.TemplatesInserted = inserted;
                counts.TemplatesUpdated  = updated;
            });
            anyFailed |= !await DrainAsync("variants",  opened.RunId, ct, async () =>
            {
                int seen = 0, inserted = 0, updated = 0;
                await foreach (var page in _client.StreamVariantsAsync(ct))
                {
                    var w = await _writer.UpsertVariantsAsync(page, opened.RunId, ct);
                    seen += page.Count; inserted += w.Inserted; updated += w.Updated;
                }
                counts.VariantsSeen   = seen;
                counts.CombosInserted = inserted;
                counts.CombosUpdated  = updated;
            });

            if (anyFailed)
            {
                _log.LogWarning("Spellbook refresh {RunId} finished with partial failures; skipping soft-mark sweep", opened.RunId);
                await _runs.CloseRunAsync(opened.RunId, "partial", counts.ToCloseCounts(), error: null, ct);
                return;
            }

            counts.CombosSoftDeleted = await _writer.SoftMarkUnseenAsync(opened.RunId, ct);
            await _writer.PurgeStaleCacheAsync(opened.RunId, ct);
            await _runs.CloseRunAsync(opened.RunId, "success", counts.ToCloseCounts(), error: null, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Spellbook refresh {RunId} threw", opened.RunId);
            await _runs.CloseRunAsync(opened.RunId, "failed", counts.ToCloseCounts(), error: ex.Message, ct);
            throw;
        }
    }

    private async Task<bool> DrainAsync(string entity, long runId, CancellationToken ct, Func<Task> body)
    {
        try
        {
            await body();
            return true;
        }
        catch (SpellbookPageException ex)
        {
            _log.LogWarning(ex, "Spellbook refresh {RunId}: {Entity} drain failed", runId, entity);
            return false;
        }
    }

    private sealed class MutableCounts
    {
        public int? VariantsSeen, FeaturesSeen, TemplatesSeen;
        public int? CombosInserted, CombosUpdated, CombosSoftDeleted;
        public int? FeaturesInserted, FeaturesUpdated;
        public int? TemplatesInserted, TemplatesUpdated;

        public RunCloseCounts ToCloseCounts() => new(
            VariantsSeen, FeaturesSeen, TemplatesSeen,
            CombosInserted, CombosUpdated, CombosSoftDeleted,
            FeaturesInserted, FeaturesUpdated,
            TemplatesInserted, TemplatesUpdated);
    }
}
