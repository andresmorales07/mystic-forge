using Microsoft.Extensions.Hosting;
using MysticForge.Application.Tagging;

namespace MysticForge.Infrastructure.Tagging;

/// <summary>
/// Hosted service that ensures the TaxonomyCache is populated at app startup.
/// Runs after AutoSeedHostedService (registration order in DI controls this).
/// </summary>
public sealed class TaxonomyCacheLoader : IHostedService
{
    private readonly ITaxonomyCache _cache;

    public TaxonomyCacheLoader(ITaxonomyCache cache) { _cache = cache; }

    public Task StartAsync(CancellationToken ct) => _cache.ReloadAsync(ct);
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
