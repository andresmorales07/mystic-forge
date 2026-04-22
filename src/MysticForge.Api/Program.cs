using Hangfire;
using MysticForge.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddMysticForgeInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Mystic Forge API");

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapHealthChecks("/healthz");

app.Run();
