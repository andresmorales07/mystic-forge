using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;
using MysticForge.Domain.Tags;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure.Seeding;

public sealed class TaxonomySeeder : ITaxonomySeeder
{
    private readonly MysticForgeDbContext _db;
    private readonly ITaxonomyV1YamlParser _parser;
    private readonly string _yaml;

    public TaxonomySeeder(MysticForgeDbContext db, ITaxonomyV1YamlParser parser, string yaml)
    {
        _db = db;
        _parser = parser;
        _yaml = yaml;
    }

    public async Task<TaxonomySeedResult> SeedAsync(CancellationToken ct)
    {
        var doc = _parser.Parse(_yaml);

        // Pre-load existing rows so we can compute insert/update counts cheaply.
        var existingByPath = await _db.SynergyHooks.AsNoTracking()
            .ToDictionaryAsync(h => h.Path, ct);

        // Upsert nodes in depth order so a parent's id is known before we resolve a child's ParentId.
        var assignedIds = new Dictionary<string, long>(StringComparer.Ordinal);
        var inserted = 0;
        var updated = 0;
        foreach (var node in doc.Hooks.OrderBy(h => h.Depth).ThenBy(h => h.Path))
        {
            var parentId = node.ParentPath is null ? (long?)null : assignedIds[node.ParentPath];

            if (existingByPath.TryGetValue(node.Path, out var existing))
            {
                await _db.SynergyHooks.Where(h => h.Id == existing.Id).ExecuteUpdateAsync(s => s
                    .SetProperty(h => h.Name, node.Name)
                    .SetProperty(h => h.Description, node.Description)
                    .SetProperty(h => h.SortOrder, node.SortOrder)
                    .SetProperty(h => h.Depth, node.Depth)
                    .SetProperty(h => h.ParentId, parentId)
                    .SetProperty(h => h.UpdatedAt, DateTimeOffset.UtcNow), ct);
                assignedIds[node.Path] = existing.Id;
                updated++;
            }
            else
            {
                var entity = new SynergyHook
                {
                    Path = node.Path,
                    Name = node.Name,
                    ParentId = parentId,
                    Depth = node.Depth,
                    Description = node.Description,
                    SortOrder = node.SortOrder,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                _db.SynergyHooks.Add(entity);
                await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.Clear();
                assignedIds[node.Path] = entity.Id;
                inserted++;
            }
        }

        // Upsert taxonomy_metadata singleton row (id = 1).
        var existingMeta = await _db.TaxonomyMetadata.AsNoTracking().SingleOrDefaultAsync(ct);
        if (existingMeta is null)
        {
            _db.TaxonomyMetadata.Add(new TaxonomyMetadata
            {
                Id = 1,
                TaxonomyVersion = doc.TaxonomyVersion,
                SeededAt = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            await _db.TaxonomyMetadata.Where(m => m.Id == 1).ExecuteUpdateAsync(s => s
                .SetProperty(m => m.TaxonomyVersion, doc.TaxonomyVersion)
                .SetProperty(m => m.SeededAt, DateTimeOffset.UtcNow), ct);
        }

        return new TaxonomySeedResult(inserted, updated, doc.TaxonomyVersion);
    }
}
