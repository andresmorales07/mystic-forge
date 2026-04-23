using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Infrastructure.Persistence.Entities;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class ScryfallIngestRunConfiguration : IEntityTypeConfiguration<ScryfallIngestRun>
{
    public void Configure(EntityTypeBuilder<ScryfallIngestRun> builder)
    {
        builder.ToTable("scryfall_ingest_runs");
        builder.HasKey(r => r.RunId);
        builder.Property(r => r.RunId).UseIdentityAlwaysColumn();
        builder.Property(r => r.BulkType).IsRequired();
        builder.Property(r => r.ScryfallUpdatedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(r => r.StartedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(r => r.CompletedAt).HasColumnType("timestamptz");
    }
}
