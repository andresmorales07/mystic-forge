using Microsoft.EntityFrameworkCore;

namespace MysticForge.Infrastructure.Persistence;

public sealed class MysticForgeDbContext : DbContext
{
    public MysticForgeDbContext(DbContextOptions<MysticForgeDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MysticForgeDbContext).Assembly);
    }
}
