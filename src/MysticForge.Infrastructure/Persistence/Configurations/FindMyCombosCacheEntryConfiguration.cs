using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Spellbook;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class FindMyCombosCacheEntryConfiguration : IEntityTypeConfiguration<FindMyCombosCacheEntry>
{
    public void Configure(EntityTypeBuilder<FindMyCombosCacheEntry> b)
    {
        b.ToTable("find_my_combos_cache");
        b.HasKey(c => c.DeckHash);
        b.Property(c => c.DeckHash)     .HasColumnType("bytea").IsRequired();
        b.Property(c => c.ResponseJson) .HasColumnName("response").HasColumnType("jsonb").IsRequired();
        b.Property(c => c.IngestRunId)  .IsRequired();
        b.Property(c => c.ComputedAt)   .HasColumnType("timestamptz").HasDefaultValueSql("now()");

        b.HasOne<SpellbookIngestRun>()
            .WithMany()
            .HasForeignKey(c => c.IngestRunId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(c => c.IngestRunId).HasDatabaseName("find_my_combos_cache_ingest_run_idx");
    }
}
