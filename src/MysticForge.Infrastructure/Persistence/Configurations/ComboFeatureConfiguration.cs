using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Spellbook;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class ComboFeatureConfiguration : IEntityTypeConfiguration<ComboFeature>
{
    public void Configure(EntityTypeBuilder<ComboFeature> b)
    {
        b.ToTable("combo_features");
        b.HasKey(cf => new { cf.ComboId, cf.FeatureId });

        b.HasOne(cf => cf.Combo)
            .WithMany(c => c.Features)
            .HasForeignKey(cf => cf.ComboId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(cf => cf.Feature)
            .WithMany()
            .HasForeignKey(cf => cf.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(cf => cf.FeatureId).HasDatabaseName("combo_features_feature_id_idx");
    }
}
