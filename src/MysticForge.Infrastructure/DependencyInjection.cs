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

        // optionsLifetime: Singleton lets the singleton IDbContextFactory (below) consume the same options.
        services.AddDbContext<MysticForgeDbContext>(
            options => options
                .UseNpgsql(connectionString, npg => npg.UseVector())
                .UseSnakeCaseNamingConvention(),
            optionsLifetime: ServiceLifetime.Singleton);

        // Factory used by components that must operate on their own context (e.g. IngestRunTracker
        // needs to persist failure rows even when the scoped context is polluted by a writer error).
        services.AddDbContextFactory<MysticForgeDbContext>(
            options => options
                .UseNpgsql(connectionString, npg => npg.UseVector())
                .UseSnakeCaseNamingConvention(),
            lifetime: ServiceLifetime.Singleton);

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            // Globally enforce: only one execution of any given recurring job at a time (prevents
            // retries from racing with an in-flight invocation and writing the same scryfall_id
            // twice), and cap auto-retry at 2 attempts — failures here generally indicate a real
            // issue worth investigating, not something to paper over with silent retries.
            .UseFilter(new Hangfire.DisableConcurrentExecutionAttribute(timeoutInSeconds: 600))
            .UseFilter(new Hangfire.AutomaticRetryAttribute { Attempts = 2, DelaysInSeconds = [60, 300] })
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

        // ---- Phase 2b: tagging pipeline ----

        services.AddOptions<MysticForge.Application.Tagging.TagDrainOptions>()
            .Bind(configuration.GetSection(MysticForge.Application.Tagging.TagDrainOptions.SectionName));

        var openRouterBaseUrl = configuration["OpenRouter:BaseUrl"]
            ?? throw new InvalidOperationException("OpenRouter:BaseUrl is not configured.");
        var openRouterApiKey = configuration["OpenRouter:ApiKey"] ?? string.Empty;
        var openRouterModel = configuration["OpenRouter:TaggingModel"]
            ?? throw new InvalidOperationException("OpenRouter:TaggingModel is not configured.");

        // Typed HttpClient is transient by default — every resolution gets a fresh instance from
        // IHttpClientFactory. We use AddTypedClient to construct AND Configure() in one step so
        // every materialized instance has model+apiKey set, not just the first one.
        services.AddHttpClient<MysticForge.Application.Tagging.IOpenRouterTaggingClient, MysticForge.Infrastructure.Tagging.OpenRouterTaggingClient>(http =>
        {
            http.BaseAddress = new Uri(openRouterBaseUrl.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/AndresT/MysticForge");
            http.DefaultRequestHeaders.Add("X-Title", "Mystic Forge");
        })
        .AddTypedClient<MysticForge.Application.Tagging.IOpenRouterTaggingClient>((http, sp) =>
        {
            var client = new MysticForge.Infrastructure.Tagging.OpenRouterTaggingClient(
                http,
                sp.GetRequiredService<MysticForge.Application.Tagging.IPromptBuilder>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MysticForge.Infrastructure.Tagging.OpenRouterTaggingClient>>());
            client.Configure(openRouterModel, openRouterApiKey);
            return client;
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<MysticForge.Application.Tagging.ITaxonomyCache, MysticForge.Infrastructure.Tagging.TaxonomyCache>();
        services.AddSingleton<MysticForge.Application.Tagging.IPromptBuilder, MysticForge.Infrastructure.Tagging.PromptBuilder>();
        services.AddSingleton<MysticForge.Application.Tagging.ITaxonomyV1YamlParser, MysticForge.Infrastructure.Seeding.TaxonomyV1YamlParser>();
        services.AddSingleton<MysticForge.Application.Tagging.IMechanicsRegistry, MysticForge.Infrastructure.Tagging.MechanicsRegistry>();

        services.AddScoped<MysticForge.Application.Tagging.ICardReader, MysticForge.Infrastructure.Persistence.CardReader>();
        services.AddScoped<MysticForge.Application.Tagging.IOutboxClaimer, MysticForge.Infrastructure.Persistence.OutboxClaimer>();
        services.AddScoped<MysticForge.Application.Tagging.ITagSetResolver, MysticForge.Infrastructure.Tagging.TagSetResolver>();
        services.AddScoped<MysticForge.Application.Tagging.ITaggingFailureLogger, MysticForge.Infrastructure.Persistence.TaggingFailureLogger>();
        services.AddScoped<MysticForge.Application.Tagging.ITagWriter>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<MysticForge.Infrastructure.Persistence.MysticForgeDbContext>>();
            return new MysticForge.Infrastructure.Persistence.TagWriter(() => factory.CreateDbContext());
        });

        services.AddScoped<MysticForge.Application.Tagging.TagDrainJob>();

        // Hosted services — registration order matters; they run sequentially at startup.
        // 1) Auto-seed taxonomy if synergy_hooks is empty.
        services.AddHostedService(sp =>
        {
            var yamlPath = Path.Combine(AppContext.BaseDirectory, "Seeding", "taxonomy-v1.yaml");
            return new MysticForge.Infrastructure.Seeding.AutoSeedHostedService(
                sp,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MysticForge.Infrastructure.Seeding.AutoSeedHostedService>>(),
                yamlPath);
        });
        // 2) Warm the in-memory taxonomy cache after seed.
        services.AddHostedService<MysticForge.Infrastructure.Tagging.TaxonomyCacheLoader>();

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

    public static void RegisterTagDrainRecurringJob(IServiceProvider services, TimeSpan interval)
    {
        var manager = services.GetRequiredService<IRecurringJobManager>();
        // Hangfire cron is 1-minute granularity. We round the configured interval up
        // to the nearest minute (minimum 1) and use a */N pattern.
        var minutes = Math.Max(1, (int)Math.Floor(interval.TotalMinutes));
        var cron = $"*/{minutes} * * * *";
        manager.AddOrUpdate<MysticForge.Application.Tagging.TagDrainJob>(
            recurringJobId: "tagging.drain",
            methodCall: job => job.RunAsync(CancellationToken.None),
            cronExpression: cron,
            options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
