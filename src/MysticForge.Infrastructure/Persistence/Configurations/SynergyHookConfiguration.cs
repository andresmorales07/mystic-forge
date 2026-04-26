using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Tags;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class SynergyHookConfiguration : IEntityTypeConfiguration<SynergyHook>
{
    public void Configure(EntityTypeBuilder<SynergyHook> builder)
    {
        builder.ToTable("synergy_hooks");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).UseIdentityAlwaysColumn();

        builder.Property(h => h.Path).IsRequired();
        builder.HasIndex(h => h.Path).IsUnique();

        builder.Property(h => h.Name).IsRequired();
        builder.Property(h => h.ParentId);
        builder.HasIndex(h => h.ParentId);

        builder.Property(h => h.Depth).IsRequired();
        builder.Property(h => h.Description).IsRequired();
        builder.Property(h => h.SortOrder).HasDefaultValue(0);

        builder.Property(h => h.CreatedAt).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(h => h.UpdatedAt).IsRequired().HasColumnType("timestamptz").HasDefaultValueSql("now()");

        // Self-referential FK for adjacency.
        builder.HasOne<SynergyHook>()
            .WithMany()
            .HasForeignKey(h => h.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
