using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ExchangeLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ahelp_messages");

            migrationBuilder.DropTable(
                name: "ahelp_exchanges");

            migrationBuilder.CreateTable(
                name: "support_exchanges",
                columns: table => new
                {
                    support_exchange_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    support_round = table.Column<int>(type: "INTEGER", nullable: false),
                    support_target = table.Column<Guid>(type: "TEXT", nullable: false),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_exchanges", x => x.support_exchange_id);
                });

            migrationBuilder.CreateTable(
                name: "support_messages",
                columns: table => new
                {
                    support_exchange_id = table.Column<int>(type: "INTEGER", nullable: false),
                    support_message_id = table.Column<int>(type: "INTEGER", nullable: false),
                    time_sent = table.Column<DateTime>(type: "TEXT", nullable: false),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: true),
                    SupportData = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_messages", x => new { x.support_exchange_id, x.support_message_id });
                    table.ForeignKey(
                        name: "FK_support_messages_player_player_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_support_messages_support_exchanges_support_exchange_id",
                        column: x => x.support_exchange_id,
                        principalTable: "support_exchanges",
                        principalColumn: "support_exchange_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_support_exchanges_support_round",
                table: "support_exchanges",
                column: "support_round");

            migrationBuilder.CreateIndex(
                name: "IX_support_messages_player_user_id",
                table: "support_messages",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_messages_time_sent",
                table: "support_messages",
                column: "time_sent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_messages");

            migrationBuilder.DropTable(
                name: "support_exchanges");

            migrationBuilder.CreateTable(
                name: "ahelp_exchanges",
                columns: table => new
                {
                    ahelp_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ahelp_round = table.Column<int>(type: "INTEGER", nullable: false),
                    ahelp_target = table.Column<Guid>(type: "TEXT", nullable: false),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false)
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
                    sender = table.Column<Guid>(type: "TEXT", nullable: false),
                    admins_online = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_adminned = table.Column<bool>(type: "INTEGER", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    round_status = table.Column<string>(type: "TEXT", nullable: false),
                    sender_entity = table.Column<int>(type: "INTEGER", nullable: true),
                    sender_entity_name = table.Column<string>(type: "TEXT", nullable: true),
                    target_online = table.Column<bool>(type: "INTEGER", nullable: false),
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
                name: "IX_ahelp_messages_time_sent",
                table: "ahelp_messages",
                column: "time_sent");
        }
    }
}
