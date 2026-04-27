using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MysticForge.Application.Tagging;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure.Seeding;

public sealed class AutoSeedHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AutoSeedHostedService> _log;
    private readonly string _yamlPath;

    public AutoSeedHostedService(IServiceProvider services, ILogger<AutoSeedHostedService> log, string yamlPath)
    {
        _services = services;
        _log = log;
        _yamlPath = yamlPath;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MysticForgeDbContext>();

        var hookCount = await db.SynergyHooks.CountAsync(ct);
        if (hookCount > 0)
        {
            _log.LogInformation("Skipping taxonomy seed: synergy_hooks already populated ({Count}).", hookCount);
            return;
        }

        var parser = scope.ServiceProvider.GetRequiredService<ITaxonomyV1YamlParser>();
        var yaml = await File.ReadAllTextAsync(_yamlPath, ct);
        var seeder = new TaxonomySeeder(db, parser, yaml);
        var result = await seeder.SeedAsync(ct);
        _log.LogInformation(
            "Auto-seeded taxonomy version {Version}: {Inserted} hooks inserted, {Updated} updated.",
            result.TaxonomyVersion, result.Inserted, result.Updated);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
