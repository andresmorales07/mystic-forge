using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Spellbook;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> b)
    {
        b.ToTable("templates");
        b.HasKey(t => t.Id);
        b.Property(t => t.Id)            .ValueGeneratedNever();
        b.Property(t => t.Name)          .IsRequired();

        b.Property(t => t.LastSeenRunId) .IsRequired();
        b.Property(t => t.DeletedAt)     .HasColumnType("timestamptz");
        b.Property(t => t.CreatedAt)     .HasColumnType("timestamptz").HasDefaultValueSql("now()");
        b.Property(t => t.UpdatedAt)     .HasColumnType("timestamptz").HasDefaultValueSql("now()");

        b.HasIndex(t => t.LastSeenRunId) .HasDatabaseName("templates_last_seen_run_idx");

        b.HasOne<SpellbookIngestRun>().WithMany()
            .HasForeignKey(t => t.LastSeenRunId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
