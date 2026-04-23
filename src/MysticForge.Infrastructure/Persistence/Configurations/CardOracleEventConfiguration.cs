using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class CardOracleEventConfiguration : IEntityTypeConfiguration<CardOracleEvent>
{
    public void Configure(EntityTypeBuilder<CardOracleEvent> builder)
    {
        builder.ToTable("card_oracle_events");
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).UseIdentityAlwaysColumn();

        builder.Property(e => e.OracleId).IsRequired();
        builder.Property(e => e.EventType).IsRequired();
        builder.Property(e => e.PreviousHash).HasColumnType("bytea");
        builder.Property(e => e.NewHash).IsRequired().HasColumnType("bytea");

        builder.Property(e => e.ObservedAt).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(e => e.ConsumedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => e.ObservedAt)
            .HasDatabaseName("card_oracle_events_unconsumed_idx")
            .HasFilter("consumed_at IS NULL");

        builder.HasOne<Card>()
            .WithMany()
            .HasForeignKey(e => e.OracleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
