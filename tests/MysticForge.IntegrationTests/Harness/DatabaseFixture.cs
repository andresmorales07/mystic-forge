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
        // EnableSensitiveDataLogging + EnableDetailedErrors surface parameter values and
        // the offending entity state in exceptions — critical for diagnosing live-data ingest
        // failures. Tests only; never for production.
        DbOptions = new DbContextOptionsBuilder<MysticForgeDbContext>()
            .UseNpgsql(ConnectionString + ";Include Error Detail=true", npg => npg.UseVector())
            .UseSnakeCaseNamingConvention()
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        await using var db = new MysticForgeDbContext(DbOptions);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public MysticForgeDbContext NewContext() => new(DbOptions);

    public IDbContextFactory<MysticForgeDbContext> ContextFactory => new TestDbContextFactory(DbOptions);
}
