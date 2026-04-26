using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class TaxonomyMetadataConfiguration : IEntityTypeConfiguration<TaxonomyMetadata>
{
    public void Configure(EntityTypeBuilder<TaxonomyMetadata> builder)
    {
        builder.ToTable("taxonomy_metadata", t => t.HasCheckConstraint(
            "taxonomy_metadata_singleton_chk",
            "id = 1"));
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever().HasDefaultValue(1);
        builder.Property(m => m.TaxonomyVersion).IsRequired();
        builder.Property(m => m.SeededAt).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
    }
}
