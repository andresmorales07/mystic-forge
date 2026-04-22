using Hangfire;
using MysticForge.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMysticForgeInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "Mystic Forge API");

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.Run();
