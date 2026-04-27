using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using MysticForge.Domain.Events;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class CardOracleEventConfiguration : IEntityTypeConfiguration<CardOracleEvent>
{
    public void Configure(EntityTypeBuilder<CardOracleEvent> builder)
    {
        builder.ToTable("card_oracle_events", t => t.HasCheckConstraint(
            "card_oracle_events_event_type_chk",
            "event_type IN ('created', 'errata', 'model_bump', 'taxonomy_bump')"));
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).UseIdentityAlwaysColumn();

        builder.Property(e => e.OracleId).IsRequired();
        builder.Property(e => e.EventType).IsRequired();
        builder.Property(e => e.PreviousHash).HasColumnType("bytea");
        builder.Property(e => e.NewHash).IsRequired().HasColumnType("bytea");

        builder.Property(e => e.ObservedAt).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(e => e.ConsumedAt).HasColumnType("timestamptz");

        // Phase 2b: claim columns.
        builder.Property(e => e.ClaimedAt).HasColumnType("timestamptz");
        builder.Property(e => e.ClaimedBy);
        builder.Property(e => e.ClaimAttempts).HasDefaultValue((short)0);

        builder.HasIndex(e => e.ObservedAt)
            .HasDatabaseName("card_oracle_events_unconsumed_idx")
            .HasFilter("consumed_at IS NULL");

        // Drives the claim query: "find unconsumed events whose claim has expired or never started".
        // Single-column on ClaimedAt because consumed_at is always NULL inside the partial index.
        builder.HasIndex(e => e.ClaimedAt)
            .HasDatabaseName("card_oracle_events_claim_idx")
            .HasFilter("consumed_at IS NULL");

        builder.HasOne<Card>()
            .WithMany()
            .HasForeignKey(e => e.OracleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
