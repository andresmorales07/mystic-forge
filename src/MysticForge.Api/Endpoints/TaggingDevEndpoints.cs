using MysticForge.Application.Tagging;
using MysticForge.Infrastructure.Persistence;
using MysticForge.Infrastructure.Seeding;

namespace MysticForge.Api.Endpoints;

public static class TaggingDevEndpoints
{
    public static IEndpointRouteBuilder MapTaggingDevEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/dev/seed-taxonomy", async (
            MysticForgeDbContext db,
            ITaxonomyV1YamlParser parser,
            ITaxonomyCache cache,
            IWebHostEnvironment env,
            CancellationToken ct) =>
        {
            var yamlPath = Path.Combine(AppContext.BaseDirectory, "Seeding", "taxonomy-v1.yaml");
            if (!File.Exists(yamlPath))
            {
                yamlPath = Path.Combine(env.ContentRootPath, "..", "MysticForge.Infrastructure", "Seeding", "taxonomy-v1.yaml");
            }
            if (!File.Exists(yamlPath))
                return Results.Problem(detail: $"taxonomy-v1.yaml not found at {yamlPath}", statusCode: 500);

            var yaml = await File.ReadAllTextAsync(yamlPath, ct);
            var seeder = new TaxonomySeeder(db, parser, yaml);
            var result = await seeder.SeedAsync(ct);
            await cache.ReloadAsync(ct);

            return Results.Ok(new
            {
                taxonomyVersion = result.TaxonomyVersion,
                inserted = result.Inserted,
                updated = result.Updated,
            });
        });

        return builder;
    }
}
