using Microsoft.EntityFrameworkCore;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;
using MysticForge.Infrastructure.Persistence.Entities;

namespace MysticForge.Infrastructure.Persistence;

public sealed class MysticForgeDbContext : DbContext
{
    public MysticForgeDbContext(DbContextOptions<MysticForgeDbContext> options) : base(options) { }

    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Printing> Printings => Set<Printing>();
    public DbSet<CardOracleEvent> CardOracleEvents => Set<CardOracleEvent>();
    public DbSet<ScryfallIngestRun> ScryfallIngestRuns => Set<ScryfallIngestRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MysticForgeDbContext).Assembly);
    }
}
