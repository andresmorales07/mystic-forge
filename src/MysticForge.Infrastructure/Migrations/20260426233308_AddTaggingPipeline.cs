using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MysticForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaggingPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "claim_attempts",
                table: "card_oracle_events",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "claimed_at",
                table: "card_oracle_events",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "claimed_by",
                table: "card_oracle_events",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "card_roles",
                columns: table => new
                {
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    model_version = table.Column<string>(type: "text", nullable: false),
                    taxonomy_version = table.Column<string>(type: "text", nullable: false),
                    tagged_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_roles", x => new { x.oracle_id, x.role });
                    table.CheckConstraint("card_roles_source_chk", "source IN ('llm', 'human')");
                    table.ForeignKey(
                        name: "fk_card_roles_cards_oracle_id",
                        column: x => x.oracle_id,
                        principalTable: "cards",
                        principalColumn: "oracle_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "card_tribal_interest",
                columns: table => new
                {
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creature_type = table.Column<string>(type: "text", nullable: false),
                    model_version = table.Column<string>(type: "text", nullable: false),
                    taxonomy_version = table.Column<string>(type: "text", nullable: false),
                    tagged_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_tribal_interest", x => new { x.oracle_id, x.creature_type });
                    table.CheckConstraint("card_tribal_interest_source_chk", "source IN ('llm', 'human')");
                    table.ForeignKey(
                        name: "fk_card_tribal_interest_cards_oracle_id",
                        column: x => x.oracle_id,
                        principalTable: "cards",
                        principalColumn: "oracle_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mechanics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    reviewed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mechanics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "synergy_hooks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    path = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    depth = table.Column<short>(type: "smallint", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_synergy_hooks", x => x.id);
                    table.ForeignKey(
                        name: "fk_synergy_hooks_synergy_hooks_parent_id",
                        column: x => x.parent_id,
                        principalTable: "synergy_hooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tag_failures",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    error_kind = table.Column<string>(type: "text", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: false),
                    attempts = table.Column<short>(type: "smallint", nullable: false),
                    model_version = table.Column<string>(type: "text", nullable: false),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_failures", x => x.id);
                    table.ForeignKey(
                        name: "fk_tag_failures_card_oracle_events_event_id",
                        column: x => x.event_id,
                        principalTable: "card_oracle_events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "taxonomy_metadata",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    taxonomy_version = table.Column<string>(type: "text", nullable: false),
                    seeded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxonomy_metadata", x => x.id);
                    table.CheckConstraint("taxonomy_metadata_singleton_chk", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "card_mechanics",
                columns: table => new
                {
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mechanic_id = table.Column<long>(type: "bigint", nullable: false),
                    model_version = table.Column<string>(type: "text", nullable: false),
                    taxonomy_version = table.Column<string>(type: "text", nullable: false),
                    tagged_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_mechanics", x => new { x.oracle_id, x.mechanic_id });
                    table.CheckConstraint("card_mechanics_source_chk", "source IN ('llm', 'human')");
                    table.ForeignKey(
                        name: "fk_card_mechanics_cards_oracle_id",
                        column: x => x.oracle_id,
                        principalTable: "cards",
                        principalColumn: "oracle_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_card_mechanics_mechanics_mechanic_id",
                        column: x => x.mechanic_id,
                        principalTable: "mechanics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "card_synergy_hook_ancestors",
                columns: table => new
                {
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ancestor_hook_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_synergy_hook_ancestors", x => new { x.oracle_id, x.ancestor_hook_id });
                    table.ForeignKey(
                        name: "fk_card_synergy_hook_ancestors_cards_oracle_id",
                        column: x => x.oracle_id,
                        principalTable: "cards",
                        principalColumn: "oracle_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_card_synergy_hook_ancestors_synergy_hooks_ancestor_hook_id",
                        column: x => x.ancestor_hook_id,
                        principalTable: "synergy_hooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "card_synergy_hooks",
                columns: table => new
                {
                    oracle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hook_id = table.Column<long>(type: "bigint", nullable: false),
                    model_version = table.Column<string>(type: "text", nullable: false),
                    taxonomy_version = table.Column<string>(type: "text", nullable: false),
                    tagged_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_card_synergy_hooks", x => new { x.oracle_id, x.hook_id });
                    table.CheckConstraint("card_synergy_hooks_source_chk", "source IN ('llm', 'human')");
                    table.ForeignKey(
                        name: "fk_card_synergy_hooks_cards_oracle_id",
                        column: x => x.oracle_id,
                        principalTable: "cards",
                        principalColumn: "oracle_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_card_synergy_hooks_synergy_hooks_hook_id",
                        column: x => x.hook_id,
                        principalTable: "synergy_hooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "card_oracle_events_claim_idx",
                table: "card_oracle_events",
                column: "claimed_at",
                filter: "consumed_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "card_oracle_events_event_type_chk",
                table: "card_oracle_events",
                sql: "event_type IN ('created', 'errata', 'model_bump', 'taxonomy_bump')");

            migrationBuilder.CreateIndex(
                name: "ix_card_mechanics_mechanic_id",
                table: "card_mechanics",
                column: "mechanic_id");

            migrationBuilder.CreateIndex(
                name: "ix_card_roles_role",
                table: "card_roles",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_card_synergy_hook_ancestors_ancestor_hook_id",
                table: "card_synergy_hook_ancestors",
                column: "ancestor_hook_id");

            migrationBuilder.CreateIndex(
                name: "ix_card_synergy_hooks_hook_id",
                table: "card_synergy_hooks",
                column: "hook_id");

            migrationBuilder.CreateIndex(
                name: "ix_card_tribal_interest_creature_type",
                table: "card_tribal_interest",
                column: "creature_type");

            migrationBuilder.CreateIndex(
                name: "ix_mechanics_name",
                table: "mechanics",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "mechanics_reviewed_false_idx",
                table: "mechanics",
                column: "reviewed",
                filter: "reviewed = false");

            migrationBuilder.CreateIndex(
                name: "ix_synergy_hooks_parent_id",
                table: "synergy_hooks",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_synergy_hooks_path",
                table: "synergy_hooks",
                column: "path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tag_failures_event_id",
                table: "tag_failures",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_tag_failures_failed_at",
                table: "tag_failures",
                column: "failed_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_tag_failures_oracle_id",
                table: "tag_failures",
                column: "oracle_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "card_mechanics");

            migrationBuilder.DropTable(
                name: "card_roles");

            migrationBuilder.DropTable(
                name: "card_synergy_hook_ancestors");

            migrationBuilder.DropTable(
                name: "card_synergy_hooks");

            migrationBuilder.DropTable(
                name: "card_tribal_interest");

            migrationBuilder.DropTable(
                name: "tag_failures");

            migrationBuilder.DropTable(
                name: "taxonomy_metadata");

            migrationBuilder.DropTable(
                name: "mechanics");

            migrationBuilder.DropTable(
                name: "synergy_hooks");

            migrationBuilder.DropIndex(
                name: "card_oracle_events_claim_idx",
                table: "card_oracle_events");

            migrationBuilder.DropCheckConstraint(
                name: "card_oracle_events_event_type_chk",
                table: "card_oracle_events");

            migrationBuilder.DropColumn(
                name: "claim_attempts",
                table: "card_oracle_events");

            migrationBuilder.DropColumn(
                name: "claimed_at",
                table: "card_oracle_events");

            migrationBuilder.DropColumn(
                name: "claimed_by",
                table: "card_oracle_events");
        }
    }
}
