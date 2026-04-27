using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class CardMechanicConfiguration : IEntityTypeConfiguration<CardMechanic>
{
    public void Configure(EntityTypeBuilder<CardMechanic> builder)
    {
        builder.ToTable("card_mechanics", t => t.HasCheckConstraint(
            "card_mechanics_source_chk", "source IN ('llm', 'human')"));
        builder.HasKey(m => new { m.OracleId, m.MechanicId });

        builder.HasIndex(m => m.MechanicId);

        builder.Property(m => m.ModelVersion).IsRequired();
        builder.Property(m => m.TaxonomyVersion).IsRequired();
        builder.Property(m => m.TaggedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(m => m.Source).IsRequired();

        builder.HasOne<Card>().WithMany().HasForeignKey(m => m.OracleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Mechanic>().WithMany().HasForeignKey(m => m.MechanicId).OnDelete(DeleteBehavior.Cascade);
    }
}
