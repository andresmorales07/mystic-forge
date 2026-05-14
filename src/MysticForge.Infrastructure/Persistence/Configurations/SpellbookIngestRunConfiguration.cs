using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Spellbook;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class SpellbookIngestRunConfiguration : IEntityTypeConfiguration<SpellbookIngestRun>
{
    public void Configure(EntityTypeBuilder<SpellbookIngestRun> b)
    {
        b.ToTable("spellbook_ingest_runs");
        b.HasKey(r => r.RunId);
        b.Property(r => r.RunId)        .UseIdentityAlwaysColumn();   // BIGSERIAL
        b.Property(r => r.StartedAt)    .IsRequired().HasColumnType("timestamptz");
        b.Property(r => r.CompletedAt)  .HasColumnType("timestamptz");

        b.HasIndex(r => r.RunId)
            .HasFilter("outcome = 'success'")
            .IsDescending(true)
            .HasDatabaseName("spellbook_ingest_runs_success_idx");
    }
}
