using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdminLogV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_log_player");

            migrationBuilder.DropTable(
                name: "admin_log");

            migrationBuilder.CreateTable(
                name: "admin_log_entity_dimension",
                columns: table => new
                {
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    entity_uid = table.Column<int>(type: "INTEGER", nullable: false),
                    prototype_id = table.Column<string>(type: "TEXT", nullable: true),
                    entity_name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_entity_dimension", x => new { x.server_id, x.round_id, x.entity_uid });
                    table.ForeignKey(
                        name: "FK_admin_log_entity_dimension_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_event",
                columns: table => new
                {
                    admin_log_event_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false),
                    impact = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_event", x => x.admin_log_event_id);
                    table.ForeignKey(
                        name: "FK_admin_log_event_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_log_event_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_event_participant",
                columns: table => new
                {
                    admin_log_event_participant_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    event_id = table.Column<int>(type: "INTEGER", nullable: false),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false),
                    impact = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    entity_uid = table.Column<int>(type: "INTEGER", nullable: true),
                    role = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_event_participant", x => x.admin_log_event_participant_id);
                    table.ForeignKey(
                        name: "FK_admin_log_event_participant_admin_log_event_event_id",
                        column: x => x.event_id,
                        principalTable: "admin_log_event",
                        principalColumn: "admin_log_event_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_log_event_participant_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_event_payload",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "INTEGER", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_event_payload", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_admin_log_event_payload_admin_log_event_event_id",
                        column: x => x.event_id,
                        principalTable: "admin_log_event",
                        principalColumn: "admin_log_event_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_entity_dimension_server_id_entity_uid",
                table: "admin_log_entity_dimension",
                columns: new[] { "server_id", "entity_uid" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_round_id_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "round_id", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_round_id_type_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "round_id", "type", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_impact_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "impact", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_round_id_impact_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "round_id", "impact", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_round_id_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "round_id", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_type_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "type", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_event_id",
                table: "admin_log_event_participant",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id_entity_uid_occurred_at_event_id",
                table: "admin_log_event_participant",
                columns: new[] { "server_id", "entity_uid", "occurred_at", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id_entity_uid_role_occurred_at_event_id",
                table: "admin_log_event_participant",
                columns: new[] { "server_id", "entity_uid", "role", "occurred_at", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id_player_user_id_occurred_at_event_id",
                table: "admin_log_event_participant",
                columns: new[] { "server_id", "player_user_id", "occurred_at", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id_round_id_player_user_id_occurred_at_event_id",
                table: "admin_log_event_participant",
                columns: new[] { "server_id", "round_id", "player_user_id", "occurred_at", "event_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_log_entity_dimension");

            migrationBuilder.DropTable(
                name: "admin_log_event_participant");

            migrationBuilder.DropTable(
                name: "admin_log_event_payload");

            migrationBuilder.DropTable(
                name: "admin_log_event");

            migrationBuilder.CreateTable(
                name: "admin_log",
                columns: table => new
                {
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    admin_log_id = table.Column<int>(type: "INTEGER", nullable: false),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    impact = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    json = table.Column<string>(type: "jsonb", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log", x => new { x.round_id, x.admin_log_id });
                    table.ForeignKey(
                        name: "FK_admin_log_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_player",
                columns: table => new
                {
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    log_id = table.Column<int>(type: "INTEGER", nullable: false),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_player", x => new { x.round_id, x.log_id, x.player_user_id });
                    table.ForeignKey(
                        name: "FK_admin_log_player_admin_log_round_id_log_id",
                        columns: x => new { x.round_id, x.log_id },
                        principalTable: "admin_log",
                        principalColumns: new[] { "round_id", "admin_log_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_log_player_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_date",
                table: "admin_log",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_type",
                table: "admin_log",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_player_player_user_id",
                table: "admin_log_player",
                column: "player_user_id");
        }
    }
}
