using Microsoft.Extensions.Logging;
using MysticForge.Application.Tagging;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Tagging;

public sealed class TagSetResolver : ITagSetResolver
{
    private readonly ITaxonomyCache _cache;
    private readonly IMechanicsRegistry _mechanics;
    private readonly ILogger<TagSetResolver>? _log;

    public TagSetResolver(ITaxonomyCache cache, IMechanicsRegistry mechanics, ILogger<TagSetResolver>? log = null)
    {
        _cache = cache;
        _mechanics = mechanics;
        _log = log;
    }

    public async Task<ResolvedTagSet> ResolveAsync(
        Guid oracleId,
        RawTagSet raw,
        string modelVersion,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var taxonomyVersion = _cache.CurrentTaxonomyVersion;

        var roles = new List<CardRole>();
        foreach (var role in raw.Roles.Distinct(StringComparer.Ordinal))
        {
            if (!_cache.IsValidRole(role))
            {
                _log?.LogWarning("Dropping unknown role '{Role}' for card {OracleId}.", role, oracleId);
                continue;
            }
            roles.Add(new CardRole
            {
                OracleId = oracleId,
                Role = role,
                ModelVersion = modelVersion,
                TaxonomyVersion = taxonomyVersion,
                TaggedAt = now,
                Source = "llm",
            });
        }

        var hooks = new List<CardSynergyHook>();
        var ancestorIds = new HashSet<long>();
        foreach (var path in raw.SynergyHookPaths.Distinct(StringComparer.Ordinal))
        {
            if (!_cache.TryResolveHook(path, out var hookId))
            {
                _log?.LogWarning("Dropping unknown hook path '{Path}' for card {OracleId}.", path, oracleId);
                continue;
            }
            hooks.Add(new CardSynergyHook
            {
                OracleId = oracleId,
                HookId = hookId,
                ModelVersion = modelVersion,
                TaxonomyVersion = taxonomyVersion,
                TaggedAt = now,
                Source = "llm",
            });
            foreach (var anc in _cache.AncestorsOf(hookId)) ancestorIds.Add(anc);
        }

        var ancestors = ancestorIds.Select(aid => new CardSynergyHookAncestor
        {
            OracleId = oracleId,
            AncestorHookId = aid,
        }).ToList();

        var mechanicRows = new List<CardMechanic>();
        foreach (var mechName in raw.Mechanics.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.Ordinal))
        {
            var mechanicId = await _mechanics.ResolveOrInsertAsync(mechName, ct);
            mechanicRows.Add(new CardMechanic
            {
                OracleId = oracleId,
                MechanicId = mechanicId,
                ModelVersion = modelVersion,
                TaxonomyVersion = taxonomyVersion,
                TaggedAt = now,
                Source = "llm",
            });
        }
        // Dedupe by mechanicId in case two raw names normalized to the same id.
        mechanicRows = mechanicRows.GroupBy(r => r.MechanicId).Select(g => g.First()).ToList();

        var tribal = raw.TribalInterest
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.Ordinal)
            .Select(t => new CardTribalInterest
            {
                OracleId = oracleId,
                CreatureType = t,
                ModelVersion = modelVersion,
                TaxonomyVersion = taxonomyVersion,
                TaggedAt = now,
                Source = "llm",
            }).ToList();

        return new ResolvedTagSet(roles, hooks, ancestors, mechanicRows, tribal);
    }
}
