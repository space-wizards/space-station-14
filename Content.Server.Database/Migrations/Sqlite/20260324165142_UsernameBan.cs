using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UsernameBan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "username_ban_exact",
                columns: table => new
                {
                    username_ban_exact_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    custom_message = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    created_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_username_ban_exact", x => x.username_ban_exact_id);
                    table.ForeignKey(
                        name: "FK_username_ban_exact_player_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_username_ban_exact_player_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_username_ban_exact_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "username_ban_regex",
                columns: table => new
                {
                    username_ban_regex_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pattern = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    custom_message = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    auto_escalate = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_username_ban_regex", x => x.username_ban_regex_id);
                    table.ForeignKey(
                        name: "FK_username_ban_regex_player_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_username_ban_regex_player_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_username_ban_regex_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "username_ban_whitelist",
                columns: table => new
                {
                    username_ban_whitelist_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    username = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    created_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_username_ban_whitelist", x => x.username_ban_whitelist_id);
                    table.ForeignKey(
                        name: "FK_username_ban_whitelist_player_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_exact_created_by_id",
                table: "username_ban_exact",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_exact_deleted_by_id",
                table: "username_ban_exact",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_exact_last_edited_by_id",
                table: "username_ban_exact",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_exact_username",
                table: "username_ban_exact",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_regex_created_by_id",
                table: "username_ban_regex",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_regex_deleted_by_id",
                table: "username_ban_regex",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_regex_last_edited_by_id",
                table: "username_ban_regex",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_whitelist_created_by_id",
                table: "username_ban_whitelist",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_username_ban_whitelist_username",
                table: "username_ban_whitelist",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "username_ban_exact");

            migrationBuilder.DropTable(
                name: "username_ban_regex");

            migrationBuilder.DropTable(
                name: "username_ban_whitelist");
        }
    }
}
