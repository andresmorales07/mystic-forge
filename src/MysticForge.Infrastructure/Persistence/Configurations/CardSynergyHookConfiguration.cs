using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class CardSynergyHookConfiguration : IEntityTypeConfiguration<CardSynergyHook>
{
    public void Configure(EntityTypeBuilder<CardSynergyHook> builder)
    {
        builder.ToTable("card_synergy_hooks", t => t.HasCheckConstraint(
            "card_synergy_hooks_source_chk", "source IN ('llm', 'human')"));
        builder.HasKey(h => new { h.OracleId, h.HookId });

        builder.HasIndex(h => h.HookId);

        builder.Property(h => h.ModelVersion).IsRequired();
        builder.Property(h => h.TaxonomyVersion).IsRequired();
        builder.Property(h => h.TaggedAt).IsRequired().HasColumnType("timestamptz");
        builder.Property(h => h.Source).IsRequired();

        builder.HasOne<Card>().WithMany().HasForeignKey(h => h.OracleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<SynergyHook>().WithMany().HasForeignKey(h => h.HookId).OnDelete(DeleteBehavior.Restrict);
    }
}
