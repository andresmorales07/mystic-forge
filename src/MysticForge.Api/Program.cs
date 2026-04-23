using Hangfire;
using MysticForge.Api.Options;
using MysticForge.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddMysticForgeInfrastructure(builder.Configuration);

builder.Services
    .AddOptions<ScryfallOptions>()
    .Bind(builder.Configuration.GetSection(ScryfallOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<OpenRouterOptions>()
    .Bind(builder.Configuration.GetSection(OpenRouterOptions.SectionName));

builder.Services
    .AddOptions<EdhRecOptions>()
    .Bind(builder.Configuration.GetSection(EdhRecOptions.SectionName));

builder.Services
    .AddOptions<CommanderSpellbookOptions>()
    .Bind(builder.Configuration.GetSection(CommanderSpellbookOptions.SectionName));

var app = builder.Build();

MysticForge.Infrastructure.DependencyInjection.RegisterScryfallRecurringJob(app.Services, "default_cards");

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Mystic Forge API");

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");

    // Dev-only: fire the Scryfall ingest synchronously (from the caller's perspective; Hangfire
    // still queues/executes it) without going through the dashboard's form-based trigger.
    app.MapPost("/dev/trigger-ingest", (IRecurringJobManager jobs) =>
    {
        jobs.Trigger("scryfall.ingest.default-cards");
        return Results.Accepted();
    });
}

app.MapHealthChecks("/healthz");

app.Run();
