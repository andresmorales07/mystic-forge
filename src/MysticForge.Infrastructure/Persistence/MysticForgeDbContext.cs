using Microsoft.EntityFrameworkCore;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;
using MysticForge.Domain.Spellbook;
using MysticForge.Domain.Tags;
using MysticForge.Infrastructure.Persistence.Entities;

namespace MysticForge.Infrastructure.Persistence;

public sealed class MysticForgeDbContext : DbContext
{
    public MysticForgeDbContext(DbContextOptions<MysticForgeDbContext> options) : base(options) { }

    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Printing> Printings => Set<Printing>();
    public DbSet<CardOracleEvent> CardOracleEvents => Set<CardOracleEvent>();
    public DbSet<ScryfallIngestRun> ScryfallIngestRuns => Set<ScryfallIngestRun>();

    // Phase 2b — taxonomy tables.
    public DbSet<SynergyHook> SynergyHooks => Set<SynergyHook>();
    public DbSet<Mechanic> Mechanics => Set<Mechanic>();
    public DbSet<TaxonomyMetadata> TaxonomyMetadata => Set<TaxonomyMetadata>();

    // Phase 2b — link + ancestor tables.
    public DbSet<CardRole> CardRoles => Set<CardRole>();
    public DbSet<CardSynergyHook> CardSynergyHooks => Set<CardSynergyHook>();
    public DbSet<CardSynergyHookAncestor> CardSynergyHookAncestors => Set<CardSynergyHookAncestor>();
    public DbSet<CardMechanic> CardMechanics => Set<CardMechanic>();
    public DbSet<CardTribalInterest> CardTribalInterest => Set<CardTribalInterest>();

    // Phase 2b — audit.
    public DbSet<TagFailure> TagFailures => Set<TagFailure>();

    // Phase 3a — Spellbook mirror.
    public DbSet<SpellbookIngestRun>     SpellbookIngestRuns   => Set<SpellbookIngestRun>();
    public DbSet<Feature>                Features              => Set<Feature>();
    public DbSet<Template>               Templates             => Set<Template>();
    public DbSet<Combo>                  Combos                => Set<Combo>();
    public DbSet<ComboCard>              ComboCards            => Set<ComboCard>();
    public DbSet<ComboFeature>           ComboFeatures         => Set<ComboFeature>();
    public DbSet<ComboTemplate>          ComboTemplates        => Set<ComboTemplate>();
    public DbSet<FindMyCombosCacheEntry> FindMyCombosCache     => Set<FindMyCombosCacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MysticForgeDbContext).Assembly);
    }
}
