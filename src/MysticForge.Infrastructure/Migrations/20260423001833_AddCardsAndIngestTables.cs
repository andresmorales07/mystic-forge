using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace MysticForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardsAndIngestTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cards",
                columns: table => new
                {
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    layout = table.Column<string>(type: "text", nullable: false),
                    oracle_text = table.Column<string>(type: "text", nullable: true),
                    type_line = table.Column<string>(type: "text", nullable: true),
                    mana_cost = table.Column<string>(type: "text", nullable: true),
                    card_faces = table.Column<string>(type: "jsonb", nullable: true),
                    cmc = table.Column<decimal>(type: "numeric", nullable: true),
                    colors = table.Column<string[]>(type: "text[]", nullable: false),
                    color_identity = table.Column<string[]>(type: "text[]", nullable: false),
                    keywords = table.Column<string[]>(type: "text[]", nullable: false),
                    oracle_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    last_oracle_change = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cards", x => x.oracle_id);
                    table.CheckConstraint("cards_face_shape_chk", "(oracle_text IS NOT NULL AND type_line IS NOT NULL AND card_faces IS NULL) OR (oracle_text IS NULL AND type_line IS NULL AND card_faces IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "scryfall_ingest_runs",
                columns: table => new
                {
                    run_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    bulk_type = table.Column<string>(type: "text", nullable: false),
                    scryfall_updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    outcome = table.Column<string>(type: "text", nullable: true),
                    cards_inserted = table.Column<int>(type: "integer", nullable: true),
                    cards_updated = table.Column<int>(type: "integer", nullable: true),
                    printings_inserted = table.Column<int>(type: "integer", nullable: true),
                    printings_updated = table.Column<int>(type: "integer", nullable: true),
                    errata_emitted = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scryfall_ingest_runs", x => x.run_id);
                });

            migrationBuilder.CreateTable(
                name: "card_oracle_events",
                columns: table => new
                {
                    event_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    previous_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    new_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_oracle_events", x => x.event_id);
                    table.ForeignKey(
                        name: "fk_card_oracle_events_cards_oracle_id",
                        column: x => x.oracle_id,
                        principalTable: "cards",
                        principalColumn: "oracle_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "printings",
                columns: table => new
                {
                    scryfall_id = table.Column<Guid>(type: "uuid", nullable: false),
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    set_code = table.Column<string>(type: "text", nullable: false),
                    collector_number = table.Column<string>(type: "text", nullable: false),
                    rarity = table.Column<string>(type: "text", nullable: false),
                    price_usd = table.Column<decimal>(type: "numeric", nullable: true),
                    price_usd_foil = table.Column<decimal>(type: "numeric", nullable: true),
                    price_usd_etched = table.Column<decimal>(type: "numeric", nullable: true),
                    price_eur = table.Column<decimal>(type: "numeric", nullable: true),
                    price_eur_foil = table.Column<decimal>(type: "numeric", nullable: true),
                    price_tix = table.Column<decimal>(type: "numeric", nullable: true),
                    image_uri_normal = table.Column<string>(type: "text", nullable: true),
                    image_uri_small = table.Column<string>(type: "text", nullable: true),
                    scryfall_uri = table.Column<string>(type: "text", nullable: true),
                    released_at = table.Column<DateOnly>(type: "date", nullable: true),
                    last_price_update = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_printings", x => x.scryfall_id);
                    table.ForeignKey(
                        name: "fk_printings_cards_oracle_id",
                        column: x => x.oracle_id,
                        principalTable: "cards",
                        principalColumn: "oracle_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "card_oracle_events_unconsumed_idx",
                table: "card_oracle_events",
                column: "observed_at",
                filter: "consumed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_card_oracle_events_oracle_id",
                table: "card_oracle_events",
                column: "oracle_id");

            migrationBuilder.CreateIndex(
                name: "cards_color_identity_gin",
                table: "cards",
                column: "color_identity")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "cards_keywords_gin",
                table: "cards",
                column: "keywords")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "printings_oracle_id_idx",
                table: "printings",
                column: "oracle_id");

            migrationBuilder.CreateIndex(
                name: "printings_set_code_idx",
                table: "printings",
                column: "set_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_oracle_events");

            migrationBuilder.DropTable(
                name: "printings");

            migrationBuilder.DropTable(
                name: "scryfall_ingest_runs");

            migrationBuilder.DropTable(
                name: "cards");
        }
    }
}
