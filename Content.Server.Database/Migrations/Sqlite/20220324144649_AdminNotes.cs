#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class AdminNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_notes",
                columns: table => new
                {
                    admin_notes_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    created_by_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    shown_to_player = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_notes", x => x.admin_notes_id);
                    table.ForeignKey(
                        name: "FK_admin_notes_player_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_notes_player_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalTable: "player",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_admin_notes_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_notes_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_notes_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_created_by_id",
                table: "admin_notes",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_deleted_by_id",
                table: "admin_notes",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_last_edited_by_id",
                table: "admin_notes",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_player_user_id",
                table: "admin_notes",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_round_id",
                table: "admin_notes",
                column: "round_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_notes");
        }
    }
}
