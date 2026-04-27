using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class CardRoleConfiguration : IEntityTypeConfiguration<CardRole>
{
    public void Configure(EntityTypeBuilder<CardRole> builder)
    {
        builder.ToTable("card_roles", t => t.HasCheckConstraint(
            "card_roles_source_chk", "source IN ('llm', 'human')"));
        builder.HasKey(r => new { r.OracleId, r.Role });

        builder.Property(r => r.Role).IsRequired();
        builder.HasIndex(r => r.Role);

        builder.Property(r => r.ModelVersion).IsRequired();
        builder.Property(r => r.TaxonomyVersion).IsRequired();
        builder.Property(r => r.TaggedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(r => r.Source).IsRequired();

        builder.HasOne<Card>()
            .WithMany()
            .HasForeignKey(r => r.OracleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
