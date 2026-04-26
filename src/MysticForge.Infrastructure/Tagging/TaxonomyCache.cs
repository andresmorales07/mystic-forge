using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MysticForge.Application.Tagging;
using MysticForge.Domain.Tags;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure.Tagging;

public sealed class TaxonomyCache : ITaxonomyCache
{
    private readonly IDbContextFactory<MysticForgeDbContext>? _factory;
    private readonly ILogger<TaxonomyCache>? _log;
    private readonly object _lock = new();
    private Snapshot _snapshot = Snapshot.Empty;

    // Production constructor — uses DbContextFactory for reload.
    public TaxonomyCache(IDbContextFactory<MysticForgeDbContext> factory, ILogger<TaxonomyCache> log)
    {
        _factory = factory;
        _log = log;
    }

    // Test-only constructor for unit tests.
    public TaxonomyCache() { }

    public string CurrentTaxonomyVersion => _snapshot.TaxonomyVersion;
    public IReadOnlyList<SynergyHook> AllHooks => _snapshot.AllHooks;

    public bool TryResolveHook(string path, out long hookId) =>
        _snapshot.PathToHookId.TryGetValue(path, out hookId);

    public IReadOnlyList<long> AncestorsOf(long hookId) =>
        _snapshot.HookAncestors.TryGetValue(hookId, out var arr) ? arr : Array.Empty<long>();

    public bool IsValidRole(string role) => Role.All.Contains(role);

    public async Task ReloadAsync(CancellationToken ct)
    {
        if (_factory is null) throw new InvalidOperationException("TaxonomyCache constructed for testing; cannot reload.");
        await using var db = await _factory.CreateDbContextAsync(ct);
        var hooks = await db.SynergyHooks.AsNoTracking().ToListAsync(ct);
        var meta = await db.TaxonomyMetadata.AsNoTracking().SingleOrDefaultAsync(ct);
        var snap = Build(hooks, meta?.TaxonomyVersion ?? "unspecified");
        lock (_lock) { _snapshot = snap; }
        _log?.LogInformation("TaxonomyCache reloaded: version {Version}, {HookCount} hooks.", snap.TaxonomyVersion, hooks.Count);
    }

    // Test-only helper.
    public void LoadForTesting(string taxonomyVersion, IReadOnlyList<SynergyHook> hooks)
    {
        var snap = Build(hooks, taxonomyVersion);
        lock (_lock) { _snapshot = snap; }
    }

    private static Snapshot Build(IReadOnlyList<SynergyHook> hooks, string taxonomyVersion)
    {
        var byId = hooks.ToDictionary(h => h.Id);
        var pathToId = hooks.ToDictionary(h => h.Path, h => h.Id, StringComparer.Ordinal);
        var ancestors = new Dictionary<long, long[]>();
        foreach (var hook in hooks)
        {
            var chain = new List<long>();
            var current = hook.ParentId;
            while (current.HasValue)
            {
                chain.Add(current.Value);
                current = byId[current.Value].ParentId;
            }
            ancestors[hook.Id] = chain.ToArray();
        }
        return new Snapshot(taxonomyVersion, hooks, pathToId, ancestors);
    }

    private sealed record Snapshot(
        string TaxonomyVersion,
        IReadOnlyList<SynergyHook> AllHooks,
        IReadOnlyDictionary<string, long> PathToHookId,
        IReadOnlyDictionary<long, long[]> HookAncestors)
    {
        public static readonly Snapshot Empty = new(
            "unspecified",
            Array.Empty<SynergyHook>(),
            new Dictionary<string, long>(),
            new Dictionary<long, long[]>());
    }
}
