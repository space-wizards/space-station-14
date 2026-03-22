using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ahelpstodb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_help",
                columns: table => new
                {
                    admin_help_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    closed_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_help", x => x.admin_help_id);
                    table.ForeignKey(
                        name: "FK_admin_help_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_help_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_help_message",
                columns: table => new
                {
                    admin_help_message_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    admin_help_id = table.Column<int>(type: "INTEGER", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    sent_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    sender_was_admin = table.Column<bool>(type: "INTEGER", nullable: false),
                    controlled_entity_uid = table.Column<int>(type: "INTEGER", nullable: true),
                    round_state = table.Column<byte>(type: "INTEGER", nullable: false),
                    player_online_status = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_help_message", x => x.admin_help_message_id);
                    table.ForeignKey(
                        name: "FK_admin_help_message_admin_help_admin_help_id",
                        column: x => x.admin_help_id,
                        principalTable: "admin_help",
                        principalColumn: "admin_help_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_help_message_player_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_help_player_user_id",
                table: "admin_help",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_help_round_id",
                table: "admin_help",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_help_message_admin_help_id",
                table: "admin_help_message",
                column: "admin_help_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_help_message_sender_user_id",
                table: "admin_help_message",
                column: "sender_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_help_message_sent_at",
                table: "admin_help_message",
                column: "sent_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_help_message");

            migrationBuilder.DropTable(
                name: "admin_help");
        }
    }
}
