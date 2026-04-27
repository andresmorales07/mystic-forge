using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class MechanicConfiguration : IEntityTypeConfiguration<Mechanic>
{
    public void Configure(EntityTypeBuilder<Mechanic> builder)
    {
        builder.ToTable("mechanics");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).UseIdentityAlwaysColumn();

        builder.Property(m => m.Name).IsRequired();
        builder.HasIndex(m => m.Name).IsUnique();

        builder.Property(m => m.DisplayName);
        builder.Property(m => m.Reviewed).HasDefaultValue(false);

        // Partial index for "show me unreviewed mechanics" — the only access pattern that matters.
        builder.HasIndex(m => m.Reviewed)
            .HasDatabaseName("mechanics_reviewed_false_idx")
            .HasFilter("reviewed = false");

        builder.Property(m => m.FirstSeenAt).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(m => m.ReviewedAt).HasColumnType("timestamptz");
    }
}
