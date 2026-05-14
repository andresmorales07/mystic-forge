using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MysticForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpellbookMirror : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spellbook_ingest_runs",
                columns: table => new
                {
                    run_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    outcome = table.Column<string>(type: "text", nullable: true),
                    variants_seen = table.Column<int>(type: "integer", nullable: true),
                    features_seen = table.Column<int>(type: "integer", nullable: true),
                    templates_seen = table.Column<int>(type: "integer", nullable: true),
                    combos_inserted = table.Column<int>(type: "integer", nullable: true),
                    combos_updated = table.Column<int>(type: "integer", nullable: true),
                    combos_soft_deleted = table.Column<int>(type: "integer", nullable: true),
                    features_inserted = table.Column<int>(type: "integer", nullable: true),
                    features_updated = table.Column<int>(type: "integer", nullable: true),
                    templates_inserted = table.Column<int>(type: "integer", nullable: true),
                    templates_updated = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spellbook_ingest_runs", x => x.run_id);
                });

            migrationBuilder.CreateTable(
                name: "combos",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    identity = table.Column<string>(type: "text", nullable: false),
                    mana_needed = table.Column<string>(type: "text", nullable: true),
                    mana_value_needed = table.Column<decimal>(type: "numeric", nullable: true),
                    other_prerequisites = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    spoiler = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    legalities = table.Column<string>(type: "jsonb", nullable: true),
                    bracket_tag = table.Column<string>(type: "text", nullable: true),
                    popularity = table.Column<int>(type: "integer", nullable: true),
                    last_seen_run_id = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_combos", x => x.id);
                    table.ForeignKey(
                        name: "fk_combos_spellbook_ingest_runs_last_seen_run_id",
                        column: x => x.last_seen_run_id,
                        principalTable: "spellbook_ingest_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "features",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    uncountable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_seen_run_id = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_features", x => x.id);
                    table.ForeignKey(
                        name: "fk_features_spellbook_ingest_runs_last_seen_run_id",
                        column: x => x.last_seen_run_id,
                        principalTable: "spellbook_ingest_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "find_my_combos_cache",
                columns: table => new
                {
                    deck_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    response = table.Column<string>(type: "jsonb", nullable: false),
                    ingest_run_id = table.Column<long>(type: "bigint", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_find_my_combos_cache", x => x.deck_hash);
                    table.ForeignKey(
                        name: "fk_find_my_combos_cache_spellbook_ingest_runs_ingest_run_id",
                        column: x => x.ingest_run_id,
                        principalTable: "spellbook_ingest_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "templates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    scryfall_query = table.Column<string>(type: "text", nullable: true),
                    scryfall_api = table.Column<string>(type: "text", nullable: true),
                    last_seen_run_id = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_templates_spellbook_ingest_runs_last_seen_run_id",
                        column: x => x.last_seen_run_id,
                        principalTable: "spellbook_ingest_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "combo_cards",
                columns: table => new
                {
                    combo_id = table.Column<string>(type: "text", nullable: false),
                    card_position = table.Column<short>(type: "smallint", nullable: false),
                    card_name = table.Column<string>(type: "text", nullable: false),
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    must_be_commander = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    zone_locations = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_combo_cards", x => new { x.combo_id, x.card_position });
                    table.ForeignKey(
                        name: "fk_combo_cards_cards_oracle_id",
                        column: x => x.oracle_id,
                        principalTable: "cards",
                        principalColumn: "oracle_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_combo_cards_combos_combo_id",
                        column: x => x.combo_id,
                        principalTable: "combos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "combo_features",
                columns: table => new
                {
                    combo_id = table.Column<string>(type: "text", nullable: false),
                    feature_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_combo_features", x => new { x.combo_id, x.feature_id });
                    table.ForeignKey(
                        name: "fk_combo_features_combos_combo_id",
                        column: x => x.combo_id,
                        principalTable: "combos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_combo_features_features_feature_id",
                        column: x => x.feature_id,
                        principalTable: "features",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "combo_templates",
                columns: table => new
                {
                    combo_id = table.Column<string>(type: "text", nullable: false),
                    template_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_combo_templates", x => new { x.combo_id, x.template_id });
                    table.ForeignKey(
                        name: "fk_combo_templates_combos_combo_id",
                        column: x => x.combo_id,
                        principalTable: "combos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_combo_templates_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "combo_cards_oracle_idx",
                table: "combo_cards",
                column: "oracle_id",
                filter: "oracle_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "combo_cards_unresolved_idx",
                table: "combo_cards",
                column: "combo_id",
                filter: "oracle_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "combo_features_feature_id_idx",
                table: "combo_features",
                column: "feature_id");

            migrationBuilder.CreateIndex(
                name: "combo_templates_template_id_idx",
                table: "combo_templates",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "combos_active_idx",
                table: "combos",
                column: "id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "combos_identity_idx",
                table: "combos",
                column: "identity");

            migrationBuilder.CreateIndex(
                name: "combos_last_seen_run_idx",
                table: "combos",
                column: "last_seen_run_id");

            migrationBuilder.CreateIndex(
                name: "features_last_seen_run_idx",
                table: "features",
                column: "last_seen_run_id");

            migrationBuilder.CreateIndex(
                name: "features_name_active_idx",
                table: "features",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "find_my_combos_cache_ingest_run_idx",
                table: "find_my_combos_cache",
                column: "ingest_run_id");

            migrationBuilder.CreateIndex(
                name: "spellbook_ingest_runs_success_idx",
                table: "spellbook_ingest_runs",
                column: "run_id",
                descending: new bool[0],
                filter: "outcome = 'success'");

            migrationBuilder.CreateIndex(
                name: "templates_last_seen_run_idx",
                table: "templates",
                column: "last_seen_run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "combo_cards");

            migrationBuilder.DropTable(
                name: "combo_features");

            migrationBuilder.DropTable(
                name: "combo_templates");

            migrationBuilder.DropTable(
                name: "find_my_combos_cache");

            migrationBuilder.DropTable(
                name: "features");

            migrationBuilder.DropTable(
                name: "combos");

            migrationBuilder.DropTable(
                name: "templates");

            migrationBuilder.DropTable(
                name: "spellbook_ingest_runs");
        }
    }
}
