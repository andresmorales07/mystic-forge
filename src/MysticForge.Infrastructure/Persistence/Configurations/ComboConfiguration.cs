using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Spellbook;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class ComboConfiguration : IEntityTypeConfiguration<Combo>
{
    public void Configure(EntityTypeBuilder<Combo> b)
    {
        b.ToTable("combos");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id)                 .IsRequired();
        b.Property(c => c.Identity)           .IsRequired();
        b.Property(c => c.ManaValueNeeded)    .HasColumnType("numeric");
        b.Property(c => c.Status)             .IsRequired();
        b.Property(c => c.Spoiler)            .HasDefaultValue(false);
        b.Property(c => c.LegalitiesJson)     .HasColumnName("legalities").HasColumnType("jsonb");

        b.Property(c => c.LastSeenRunId)      .IsRequired();
        b.Property(c => c.DeletedAt)          .HasColumnType("timestamptz");
        b.Property(c => c.CreatedAt)          .HasColumnType("timestamptz").HasDefaultValueSql("now()");
        b.Property(c => c.UpdatedAt)          .HasColumnType("timestamptz").HasDefaultValueSql("now()");

        // Indexes
        b.HasIndex(c => c.Id)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("combos_active_idx");
        b.HasIndex(c => c.Identity)
            .HasDatabaseName("combos_identity_idx");
        b.HasIndex(c => c.LastSeenRunId)
            .HasDatabaseName("combos_last_seen_run_idx");

        b.HasOne<SpellbookIngestRun>()
            .WithMany()
            .HasForeignKey(c => c.LastSeenRunId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
