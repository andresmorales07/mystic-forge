using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class CardSynergyHookAncestorConfiguration : IEntityTypeConfiguration<CardSynergyHookAncestor>
{
    public void Configure(EntityTypeBuilder<CardSynergyHookAncestor> builder)
    {
        builder.ToTable("card_synergy_hook_ancestors");
        builder.HasKey(a => new { a.OracleId, a.AncestorHookId });

        // Critical for Phase 4 recommender: "find all cards under hook X".
        builder.HasIndex(a => a.AncestorHookId);

        builder.HasOne<Card>().WithMany().HasForeignKey(a => a.OracleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<SynergyHook>().WithMany().HasForeignKey(a => a.AncestorHookId).OnDelete(DeleteBehavior.Restrict);
    }
}
