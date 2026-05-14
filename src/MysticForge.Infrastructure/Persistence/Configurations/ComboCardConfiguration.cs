using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Spellbook;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class ComboCardConfiguration : IEntityTypeConfiguration<ComboCard>
{
    public void Configure(EntityTypeBuilder<ComboCard> b)
    {
        b.ToTable("combo_cards");
        b.HasKey(cc => new { cc.ComboId, cc.CardPosition });

        b.Property(cc => cc.ComboId)         .IsRequired();
        b.Property(cc => cc.CardName)        .IsRequired();
        b.Property(cc => cc.Quantity)        .HasDefaultValue((short)1);
        b.Property(cc => cc.MustBeCommander) .HasDefaultValue(false);

        b.HasOne(cc => cc.Combo)
            .WithMany(c => c.Cards)
            .HasForeignKey(cc => cc.ComboId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Card>()
            .WithMany()
            .HasForeignKey(cc => cc.OracleId)
            .HasPrincipalKey(c => c.OracleId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(cc => cc.OracleId)
            .HasFilter("oracle_id IS NOT NULL")
            .HasDatabaseName("combo_cards_oracle_idx");
        b.HasIndex(cc => cc.ComboId)
            .HasFilter("oracle_id IS NULL")
            .HasDatabaseName("combo_cards_unresolved_idx");
    }
}
