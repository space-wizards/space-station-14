using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddAhelpExchangeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ahelp_exchanges",
                columns: table => new
                {
                    ahelp_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ahelp_round = table.Column<int>(type: "INTEGER", nullable: false),
                    ahelp_target = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ahelp_exchanges", x => x.ahelp_id);
                    table.ForeignKey(
                        name: "FK_ahelp_exchanges_player_player_id",
                        column: x => x.ahelp_target,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ahelp_messages",
                columns: table => new
                {
                    ahelp_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ahelp_messages_id = table.Column<int>(type: "INTEGER", nullable: false),
                    sent_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    round_status = table.Column<string>(type: "TEXT", nullable: false),
                    sender = table.Column<Guid>(type: "TEXT", nullable: false),
                    sender_entity = table.Column<int>(type: "INTEGER", nullable: false),
                    is_adminned = table.Column<bool>(type: "INTEGER", nullable: false),
                    target_online = table.Column<bool>(type: "INTEGER", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    time_sent = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ahelp_messages", x => new { x.ahelp_id, x.ahelp_messages_id });
                    table.ForeignKey(
                        name: "FK_ahelp_messages_ahelp_exchanges_ahelp_id",
                        column: x => x.ahelp_id,
                        principalTable: "ahelp_exchanges",
                        principalColumn: "ahelp_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ahelp_messages_player_sender",
                        column: x => x.sender,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ahelp_participants",
                columns: table => new
                {
                    ahelp_id = table.Column<int>(type: "INTEGER", nullable: false),
                    player_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ahelp_participants", x => new { x.ahelp_id, x.player_id });
                    table.ForeignKey(
                        name: "FK_ahelp_participants_ahelp_exchanges_ahelp_id",
                        column: x => x.ahelp_id,
                        principalTable: "ahelp_exchanges",
                        principalColumn: "ahelp_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ahelp_participants_player_player_id",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_exchanges_ahelp_round",
                table: "ahelp_exchanges",
                column: "ahelp_round");

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_exchanges_ahelp_target",
                table: "ahelp_exchanges",
                column: "ahelp_target");

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_messages_sender",
                table: "ahelp_messages",
                column: "sender");

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_messages_sent_at",
                table: "ahelp_messages",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_participants_player_id",
                table: "ahelp_participants",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ahelp_messages");

            migrationBuilder.DropTable(
                name: "ahelp_participants");

            migrationBuilder.DropTable(
                name: "ahelp_exchanges");
        }
    }
}
