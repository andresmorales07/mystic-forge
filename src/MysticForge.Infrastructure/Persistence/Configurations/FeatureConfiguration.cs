using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Spellbook;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> b)
    {
        b.ToTable("features");
        b.HasKey(f => f.Id);
        b.Property(f => f.Id)            .ValueGeneratedNever();   // Spellbook owns the ids
        b.Property(f => f.Name)          .IsRequired();
        b.Property(f => f.Status)        .IsRequired();
        b.Property(f => f.Uncountable)   .HasDefaultValue(false);

        b.Property(f => f.LastSeenRunId) .IsRequired();
        b.Property(f => f.DeletedAt)     .HasColumnType("timestamptz");
        b.Property(f => f.CreatedAt)     .HasColumnType("timestamptz").HasDefaultValueSql("now()");
        b.Property(f => f.UpdatedAt)     .HasColumnType("timestamptz").HasDefaultValueSql("now()");

        b.HasIndex(f => f.Name)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("features_name_active_idx");
        b.HasIndex(f => f.LastSeenRunId)
            .HasDatabaseName("features_last_seen_run_idx");

        b.HasOne<SpellbookIngestRun>().WithMany()
            .HasForeignKey(f => f.LastSeenRunId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
