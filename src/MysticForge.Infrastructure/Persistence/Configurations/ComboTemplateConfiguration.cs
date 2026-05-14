using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Spellbook;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class ComboTemplateConfiguration : IEntityTypeConfiguration<ComboTemplate>
{
    public void Configure(EntityTypeBuilder<ComboTemplate> b)
    {
        b.ToTable("combo_templates");
        b.HasKey(ct => new { ct.ComboId, ct.TemplateId });
        b.Property(ct => ct.Quantity).HasDefaultValue((short)1);

        b.HasOne(ct => ct.Combo)
            .WithMany(c => c.Templates)
            .HasForeignKey(ct => ct.ComboId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(ct => ct.Template)
            .WithMany()
            .HasForeignKey(ct => ct.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(ct => ct.TemplateId).HasDatabaseName("combo_templates_template_id_idx");
    }
}
