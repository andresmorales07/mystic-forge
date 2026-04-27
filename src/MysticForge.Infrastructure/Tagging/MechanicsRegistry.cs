using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MysticForge.Application.Tagging;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.Infrastructure.Tagging;

public sealed class MechanicsRegistry : IMechanicsRegistry, IDisposable
{
    private readonly IDbContextFactory<MysticForgeDbContext> _factory;
    private readonly ConcurrentDictionary<string, long> _cache = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public MechanicsRegistry(IDbContextFactory<MysticForgeDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<long> ResolveOrInsertAsync(string rawName, CancellationToken ct)
    {
        var normalized = IMechanicsRegistry.Normalize(rawName);
        if (string.IsNullOrEmpty(normalized))
            throw new ArgumentException("Mechanic name normalized to empty.", nameof(rawName));

        if (_cache.TryGetValue(normalized, out var cached)) return cached;

        await _writeLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(normalized, out cached)) return cached;

            await using var db = await _factory.CreateDbContextAsync(ct);

            // The no-op "DO UPDATE SET first_seen_at = mechanics.first_seen_at" lets RETURNING fire on conflict;
            // ON CONFLICT DO NOTHING would skip RETURNING when the row already exists.
            var sql = """
                INSERT INTO mechanics (name, display_name, reviewed, first_seen_at)
                VALUES ({0}, {1}, false, now())
                ON CONFLICT (name) DO UPDATE SET first_seen_at = mechanics.first_seen_at
                RETURNING id;
                """;
            var id = await db.Database.SqlQueryRaw<long>(sql, normalized, rawName)
                .SingleAsync(ct);

            _cache[normalized] = id;
            return id;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose() => _writeLock.Dispose();
}
