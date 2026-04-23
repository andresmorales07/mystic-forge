using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MysticForge.Domain.Cards;
using Pgvector;

namespace MysticForge.Infrastructure.Persistence.Configurations;

public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards", t => t.HasCheckConstraint(
            "cards_face_shape_chk",
            "(oracle_text IS NOT NULL AND type_line IS NOT NULL AND card_faces IS NULL) " +
            "OR (oracle_text IS NULL AND type_line IS NULL AND card_faces IS NOT NULL)"));

        builder.HasKey(c => c.OracleId);

        builder.Property(c => c.Name).IsRequired();
        builder.Property(c => c.Layout).IsRequired();

        builder.Property(c => c.OracleText);
        builder.Property(c => c.TypeLine);
        builder.Property(c => c.ManaCost);

        builder.Property(c => c.Faces)
            .HasColumnName("card_faces")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<List<CardFace>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        builder.Property(c => c.Cmc).HasColumnType("numeric");

        builder.Property(c => c.Colors)
            .HasColumnType("text[]");

        builder.Property(c => c.ColorIdentity)
            .IsRequired()
            .HasColumnType("text[]");

        builder.Property(c => c.Keywords)
            .HasColumnType("text[]");

        builder.Property(c => c.OracleHash)
            .IsRequired()
            .HasColumnType("bytea");

        builder.Property(c => c.LastOracleChange)
            .IsRequired()
            .HasColumnType("timestamptz");

        // Placeholder for the v2 embedding column - kept nullable, not used in Phase 1.
        builder.Property<Vector?>("embedding")
            .HasColumnType("vector(768)");

        builder.Property<DateTimeOffset>("created_at")
            .HasDefaultValueSql("now()");
        builder.Property<DateTimeOffset>("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(c => c.ColorIdentity).HasDatabaseName("cards_color_identity_gin").HasMethod("gin");
        builder.HasIndex(c => c.Keywords).HasDatabaseName("cards_keywords_gin").HasMethod("gin");
    }
}
