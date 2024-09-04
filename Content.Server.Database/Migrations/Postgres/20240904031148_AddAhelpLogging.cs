using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddAhelpLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ahelp_exchanges",
                columns: table => new
                {
                    ahelp_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ahelp_round = table.Column<int>(type: "integer", nullable: false),
                    ahelp_target = table.Column<Guid>(type: "uuid", nullable: false),
                    server_id = table.Column<int>(type: "integer", nullable: false)
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
                    ahelp_id = table.Column<int>(type: "integer", nullable: false),
                    ahelp_messages_id = table.Column<int>(type: "integer", nullable: false),
                    time_sent = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    round_status = table.Column<string>(type: "text", nullable: false),
                    sender = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_entity = table.Column<int>(type: "integer", nullable: true),
                    sender_entity_name = table.Column<string>(type: "text", nullable: true),
                    admins_online = table.Column<bool>(type: "boolean", nullable: false),
                    is_adminned = table.Column<bool>(type: "boolean", nullable: false),
                    target_online = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ahelp_messages");

            migrationBuilder.DropTable(
                name: "ahelp_exchanges");
        }
    }
}
