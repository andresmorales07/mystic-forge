using Hangfire;
using MysticForge.Application.Spellbook;

namespace MysticForge.Api.Endpoints;

public static class SpellbookDevEndpoints
{
    public static IEndpointRouteBuilder MapSpellbookDevEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/dev/trigger-spellbook-refresh", (IBackgroundJobClient jobs) =>
        {
            var id = jobs.Enqueue<SpellbookRefreshJob>(j => j.RunAsync(CancellationToken.None));
            return Results.Accepted(value: new { jobId = id });
        });

        return builder;
    }
}
