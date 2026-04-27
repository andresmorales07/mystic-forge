using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class CardTribalInterestConfiguration : IEntityTypeConfiguration<CardTribalInterest>
{
    public void Configure(EntityTypeBuilder<CardTribalInterest> builder)
    {
        builder.ToTable("card_tribal_interest", t => t.HasCheckConstraint(
            "card_tribal_interest_source_chk", "source IN ('llm', 'human')"));
        builder.HasKey(t => new { t.OracleId, t.CreatureType });

        builder.HasIndex(t => t.CreatureType);

        builder.Property(t => t.ModelVersion).IsRequired();
        builder.Property(t => t.TaxonomyVersion).IsRequired();
        builder.Property(t => t.TaggedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(t => t.Source).IsRequired();

        builder.HasOne<Card>().WithMany().HasForeignKey(t => t.OracleId).OnDelete(DeleteBehavior.Cascade);
    }
}
