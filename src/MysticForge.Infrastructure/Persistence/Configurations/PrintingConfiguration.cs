using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class PrintingConfiguration : IEntityTypeConfiguration<Printing>
{
    public void Configure(EntityTypeBuilder<Printing> builder)
    {
        builder.ToTable("printings");
        builder.HasKey(p => p.ScryfallId);

        builder.Property(p => p.OracleId).IsRequired();
        builder.Property(p => p.SetCode).IsRequired();
        builder.Property(p => p.CollectorNumber).IsRequired();
        builder.Property(p => p.Rarity).IsRequired();

        builder.Property(p => p.PriceUsd).HasColumnType("numeric");
        builder.Property(p => p.PriceUsdFoil).HasColumnType("numeric");
        builder.Property(p => p.PriceUsdEtched).HasColumnType("numeric");
        builder.Property(p => p.PriceEur).HasColumnType("numeric");
        builder.Property(p => p.PriceEurFoil).HasColumnType("numeric");
        builder.Property(p => p.PriceTix).HasColumnType("numeric");

        builder.Property(p => p.LastPriceUpdate)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property<DateTimeOffset>("created_at").HasDefaultValueSql("now()");
        builder.Property<DateTimeOffset>("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(p => p.OracleId).HasDatabaseName("printings_oracle_id_idx");
        builder.HasIndex(p => p.SetCode).HasDatabaseName("printings_set_code_idx");

        builder.HasOne<Card>()
            .WithMany()
            .HasForeignKey(p => p.OracleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
