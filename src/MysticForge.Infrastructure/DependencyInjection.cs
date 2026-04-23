using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MysticForge.Application.Scryfall;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Scryfall;

namespace MysticForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMysticForgeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<MysticForgeDbContext>(options => options
            .UseNpgsql(connectionString, npg => npg.UseVector())
            .UseSnakeCaseNamingConvention());

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "hangfire",
                    PrepareSchemaIfNecessary = true,
                }));

        services.AddHangfireServer();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgres");

        var scryfallBaseUrl = configuration["Scryfall:BulkDataEndpoint"]
            ?? throw new InvalidOperationException("Scryfall:BulkDataEndpoint is not configured.");
        var scryfallContactEmail = configuration["Scryfall:ContactEmail"] ?? "unset@example.com";

        services.AddHttpClient<IScryfallBulkClient, ScryfallBulkClient>(http =>
        {
            // BulkDataEndpoint is "https://api.scryfall.com/bulk-data" — we want the base to be
            // "https://api.scryfall.com/" so both "bulk-data" and absolute download URIs work.
            var endpoint = new Uri(scryfallBaseUrl);
            http.BaseAddress = new Uri(endpoint.GetLeftPart(UriPartial.Authority) + "/");
            http.DefaultRequestHeaders.Add("User-Agent", $"MysticForge/0.1 (+{scryfallContactEmail})");
            http.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<IClock, Clock>();
        services.AddSingleton<IScryfallCardStreamParser, ScryfallCardStreamParser>();
        services.AddScoped<ICardWriter, CardWriter>();
        services.AddScoped<IPrintingWriter, PrintingWriter>();
        services.AddScoped<IOracleEventEmitter, OracleEventEmitter>();
        services.AddScoped<IIngestRunTracker, IngestRunTracker>();
        services.AddScoped<ScryfallIngestJob>();

        return services;
    }

    public static void RegisterScryfallRecurringJob(IServiceProvider services, string bulkType)
    {
        var manager = services.GetRequiredService<IRecurringJobManager>();
        manager.AddOrUpdate<ScryfallIngestJob>(
            recurringJobId: "scryfall.ingest.default-cards",
            methodCall: job => job.RunAsync(bulkType, CancellationToken.None),
            cronExpression: "0 10 * * *",
            options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
