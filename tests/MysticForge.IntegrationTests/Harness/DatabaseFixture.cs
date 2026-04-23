using Microsoft.EntityFrameworkCore;
using MysticForge.Infrastructure.Persistence;
using Xunit;

namespace MysticForge.IntegrationTests.Harness;

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture> { }

public sealed class DatabaseFixture : IAsyncLifetime
{
    public DatabaseFixture(PostgresContainerFixture pg)
    {
        ConnectionString = pg.ConnectionString;
    }

    public string ConnectionString { get; }

    public DbContextOptions<MysticForgeDbContext> DbOptions { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        DbOptions = new DbContextOptionsBuilder<MysticForgeDbContext>()
            .UseNpgsql(ConnectionString, npg => npg.UseVector())
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new MysticForgeDbContext(DbOptions);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public MysticForgeDbContext NewContext() => new(DbOptions);

    public IDbContextFactory<MysticForgeDbContext> ContextFactory => new TestDbContextFactory(DbOptions);
}
