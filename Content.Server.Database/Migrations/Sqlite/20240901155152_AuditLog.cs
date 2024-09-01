using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    audit_log_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    author_user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.audit_log_id);
                    table.ForeignKey(
                        name: "FK_audit_log_player__author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_log_effected_player",
                columns: table => new
                {
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    audit_log_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log_effected_player", x => x.player_user_id);
                    table.ForeignKey(
                        name: "FK_audit_log_effected_player_audit_log_audit_log_id",
                        column: x => x.audit_log_id,
                        principalTable: "audit_log",
                        principalColumn: "audit_log_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audit_log_effected_player_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log__author_user_id",
                table: "audit_log",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_date",
                table: "audit_log",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_effected_player_audit_log_id",
                table: "audit_log_effected_player",
                column: "audit_log_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_effected_player_player_user_id",
                table: "audit_log_effected_player",
                column: "player_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log_effected_player");

            migrationBuilder.DropTable(
                name: "audit_log");
        }
    }
}
