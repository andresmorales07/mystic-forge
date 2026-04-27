namespace MysticForge.Application.Tagging;

public interface ITaxonomySeeder
{
    /// <summary>
    /// Reads the taxonomy YAML and upserts synergy_hooks rows by path. Writes/updates
    /// the singleton taxonomy_metadata row. Idempotent. Returns insert/update counts
    /// for logging.
    /// </summary>
    Task<TaxonomySeedResult> SeedAsync(CancellationToken ct);
}

public sealed record TaxonomySeedResult(int Inserted, int Updated, string TaxonomyVersion);
