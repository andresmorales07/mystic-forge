using Hangfire;
using MysticForge.Api.Endpoints;
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

builder.Services
    .AddOptions<TaggingOptions>()
    .Bind(builder.Configuration.GetSection(TaggingOptions.SectionName))
    .ValidateOnStart();

var app = builder.Build();

MysticForge.Infrastructure.DependencyInjection.RegisterScryfallRecurringJob(app.Services, "default_cards");

var taggingOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<TaggingOptions>>().Value;
MysticForge.Infrastructure.DependencyInjection.RegisterTagDrainRecurringJob(app.Services, taggingOptions.DrainInterval);

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Mystic Forge API");

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");

    app.MapPost("/dev/trigger-ingest", (IRecurringJobManager jobs) =>
    {
        jobs.Trigger("scryfall.ingest.default-cards");
        return Results.Accepted();
    });

    app.MapPost("/dev/trigger-tag-drain", (IRecurringJobManager jobs) =>
    {
        jobs.Trigger("tagging.drain");
        return Results.Accepted();
    });

    app.MapTaggingDevEndpoints();
}

app.MapTaggingAdminEndpoints();

app.MapHealthChecks("/healthz");

app.Run();

// Required for WebApplicationFactory<Program> in tests.
public partial class Program { }
