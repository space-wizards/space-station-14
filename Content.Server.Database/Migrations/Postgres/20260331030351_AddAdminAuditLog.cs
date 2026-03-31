using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddAdminAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_audit_event",
                columns: table => new
                {
                    admin_audit_event_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    server_id = table.Column<int>(type: "integer", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<byte>(type: "smallint", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    target_player_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_entity_uid = table.Column<int>(type: "integer", nullable: true),
                    target_entity_name = table.Column<string>(type: "text", nullable: true),
                    target_entity_prototype = table.Column<string>(type: "text", nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "message" })
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_event", x => x.admin_audit_event_id);
                    table.ForeignKey(
                        name: "FK_admin_audit_event_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                    table.ForeignKey(
                        name: "FK_admin_audit_event_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_audit_event_payload",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    json = table.Column<JsonDocument>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_event_payload", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_admin_audit_event_payload_admin_audit_event_event_id",
                        column: x => x.event_id,
                        principalTable: "admin_audit_event",
                        principalColumn: "admin_audit_event_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_admin_user_id_occurred_at_admin_audit_eve~",
                table: "admin_audit_event",
                columns: new[] { "admin_user_id", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_round_id",
                table: "admin_audit_event",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_search_vector_gin",
                table: "admin_audit_event",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_server_id_action_occurred_at_admin_audit_~",
                table: "admin_audit_event",
                columns: new[] { "server_id", "action", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_server_id_occurred_at_admin_audit_event_id",
                table: "admin_audit_event",
                columns: new[] { "server_id", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_server_id_round_id_occurred_at_admin_audi~",
                table: "admin_audit_event",
                columns: new[] { "server_id", "round_id", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_server_id_severity_occurred_at_admin_audi~",
                table: "admin_audit_event",
                columns: new[] { "server_id", "severity", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_target_player_user_id_occurred_at_admin_a~",
                table: "admin_audit_event",
                columns: new[] { "target_player_user_id", "occurred_at", "admin_audit_event_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_event_payload");

            migrationBuilder.DropTable(
                name: "admin_audit_event");
        }
    }
}
