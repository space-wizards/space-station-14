using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class DiscordLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discord_players",
                columns: table => new
                {
                    discord_players_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ss14_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    hash_key = table.Column<string>(type: "TEXT", nullable: false),
                    ckey = table.Column<string>(type: "TEXT", nullable: false),
                    discord_id = table.Column<string>(type: "TEXT", nullable: true),
                    discord_name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discord_players", x => x.discord_players_id);
                    table.UniqueConstraint("ak_discord_players_ss14_id", x => x.ss14_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_discord_players_ckey_discord_id",
                table: "discord_players",
                columns: new[] { "ckey", "discord_id" });

            migrationBuilder.CreateIndex(
                name: "IX_discord_players_discord_players_id",
                table: "discord_players",
                column: "discord_players_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discord_players");
        }
    }
}
