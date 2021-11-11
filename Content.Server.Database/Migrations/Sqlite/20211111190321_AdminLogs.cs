using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class AdminLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "round",
                columns: table => new
                {
                    round_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_round", x => x.round_id);
                });

            migrationBuilder.CreateTable(
                name: "admin_log",
                columns: table => new
                {
                    admin_log_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    log = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log", x => x.admin_log_id);
                    table.ForeignKey(
                        name: "FK_admin_log_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_round",
                columns: table => new
                {
                    players_id = table.Column<int>(type: "INTEGER", nullable: false),
                    rounds_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_round", x => new { x.players_id, x.rounds_id });
                    table.ForeignKey(
                        name: "FK_player_round_player_players_id",
                        column: x => x.players_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_player_round_round_rounds_id",
                        column: x => x.rounds_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_player",
                columns: table => new
                {
                    admin_logs_id = table.Column<int>(type: "INTEGER", nullable: false),
                    players_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_player", x => new { x.admin_logs_id, x.players_id });
                    table.ForeignKey(
                        name: "FK_admin_log_player_admin_log_admin_logs_id",
                        column: x => x.admin_logs_id,
                        principalTable: "admin_log",
                        principalColumn: "admin_log_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_log_player_player_players_id",
                        column: x => x.players_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_round_id",
                table: "admin_log",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_player_players_id",
                table: "admin_log_player",
                column: "players_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_round_rounds_id",
                table: "player_round",
                column: "rounds_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_log_player");

            migrationBuilder.DropTable(
                name: "player_round");

            migrationBuilder.DropTable(
                name: "admin_log");

            migrationBuilder.DropTable(
                name: "round");
        }
    }
}
