using Testcontainers.PostgreSql;
using Xunit;

namespace MysticForge.IntegrationTests.Harness;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .WithDatabase("mysticforge_test")
            .WithUsername("mysticforge_test")
            .WithPassword("testpw")
            .Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Ensure extensions + hangfire schema exist (mirrors docker/postgres/init.sql)
        await _container.ExecAsync(new[]
        {
            "psql", "-U", "mysticforge_test", "-d", "mysticforge_test", "-c",
            "CREATE EXTENSION IF NOT EXISTS vector; CREATE EXTENSION IF NOT EXISTS pgcrypto; CREATE SCHEMA IF NOT EXISTS hangfire;",
        });
    }

    public async Task DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}
