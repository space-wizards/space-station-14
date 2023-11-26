using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class DiscordPlayerLinkRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_discord_players_ckey_discord_id",
                table: "discord_players");

            migrationBuilder.DropColumn(
                name: "ckey",
                table: "discord_players");

            migrationBuilder.DropColumn(
                name: "discord_name",
                table: "discord_players");

            migrationBuilder.DropColumn(
              name: "discord_id",
              table: "discord_players");

            migrationBuilder.AddColumn<decimal>(
                name: "discord_id",
                table: "discord_players",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_discord_players_discord_id",
                table: "discord_players",
                column: "discord_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_discord_players_discord_id",
                table: "discord_players");

            migrationBuilder.AlterColumn<string>(
                name: "discord_id",
                table: "discord_players",
                type: "text",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ckey",
                table: "discord_players",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "discord_name",
                table: "discord_players",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_discord_players_ckey_discord_id",
                table: "discord_players",
                columns: new[] { "ckey", "discord_id" });
        }
    }
}
