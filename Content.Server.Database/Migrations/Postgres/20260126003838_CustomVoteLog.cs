using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CustomVoteLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "custom_vote_log",
                columns: table => new
                {
                    custom_vote_log_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    time_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    initiator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_vote_log", x => x.custom_vote_log_id);
                    table.ForeignKey(
                        name: "FK_custom_vote_log_player_initiator_id1",
                        column: x => x.initiator_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_custom_vote_log_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "custom_vote_log_option",
                columns: table => new
                {
                    vote_id = table.Column<int>(type: "integer", nullable: false),
                    option_idx = table.Column<short>(type: "smallint", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    vote_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_vote_log_option", x => new { x.vote_id, x.option_idx });
                    table.ForeignKey(
                        name: "FK_custom_vote_log_option_custom_vote_log_vote_id",
                        column: x => x.vote_id,
                        principalTable: "custom_vote_log",
                        principalColumn: "custom_vote_log_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_custom_vote_log_initiator_id",
                table: "custom_vote_log",
                column: "initiator_id");

            migrationBuilder.CreateIndex(
                name: "IX_custom_vote_log_round_id",
                table: "custom_vote_log",
                column: "round_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_vote_log_option");

            migrationBuilder.DropTable(
                name: "custom_vote_log");
        }
    }
}
