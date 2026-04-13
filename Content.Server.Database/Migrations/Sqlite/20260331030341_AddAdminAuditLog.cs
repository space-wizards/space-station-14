using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
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
                    admin_audit_event_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    admin_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    action = table.Column<int>(type: "INTEGER", nullable: false),
                    severity = table.Column<byte>(type: "INTEGER", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    target_player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    target_entity_uid = table.Column<int>(type: "INTEGER", nullable: true),
                    target_entity_name = table.Column<string>(type: "TEXT", nullable: true),
                    target_entity_prototype = table.Column<string>(type: "TEXT", nullable: true),
                    message = table.Column<string>(type: "TEXT", nullable: false)
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
                    event_id = table.Column<int>(type: "INTEGER", nullable: false),
                    json = table.Column<string>(type: "jsonb", nullable: false)
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
                name: "IX_admin_audit_event_admin_user_id_occurred_at_admin_audit_event_id",
                table: "admin_audit_event",
                columns: new[] { "admin_user_id", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_round_id",
                table: "admin_audit_event",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_server_id_action_occurred_at_admin_audit_event_id",
                table: "admin_audit_event",
                columns: new[] { "server_id", "action", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_server_id_occurred_at_admin_audit_event_id",
                table: "admin_audit_event",
                columns: new[] { "server_id", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_server_id_round_id_occurred_at_admin_audit_event_id",
                table: "admin_audit_event",
                columns: new[] { "server_id", "round_id", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_server_id_severity_occurred_at_admin_audit_event_id",
                table: "admin_audit_event",
                columns: new[] { "server_id", "severity", "occurred_at", "admin_audit_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_event_target_player_user_id_occurred_at_admin_audit_event_id",
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
