using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Events;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class TagFailureConfiguration : IEntityTypeConfiguration<TagFailure>
{
    public void Configure(EntityTypeBuilder<TagFailure> builder)
    {
        builder.ToTable("tag_failures");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).UseIdentityAlwaysColumn();

        builder.Property(f => f.OracleId).IsRequired();
        builder.HasIndex(f => f.OracleId);

        builder.Property(f => f.EventId).IsRequired();
        builder.Property(f => f.ErrorKind).IsRequired();
        builder.Property(f => f.ErrorMessage).IsRequired();
        builder.Property(f => f.Attempts).IsRequired();
        builder.Property(f => f.ModelVersion).IsRequired();
        builder.Property(f => f.FailedAt).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        builder.HasIndex(f => f.FailedAt).IsDescending();

        builder.HasOne<CardOracleEvent>()
            .WithMany()
            .HasForeignKey(f => f.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
