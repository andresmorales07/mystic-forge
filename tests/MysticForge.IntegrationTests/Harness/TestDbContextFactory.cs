using Microsoft.EntityFrameworkCore;
using MysticForge.Infrastructure.Persistence;

namespace MysticForge.IntegrationTests.Harness;

public sealed class TestDbContextFactory : IDbContextFactory<MysticForgeDbContext>
{
    private readonly DbContextOptions<MysticForgeDbContext> _options;

    public TestDbContextFactory(DbContextOptions<MysticForgeDbContext> options) => _options = options;

    public MysticForgeDbContext CreateDbContext() => new(_options);
}
