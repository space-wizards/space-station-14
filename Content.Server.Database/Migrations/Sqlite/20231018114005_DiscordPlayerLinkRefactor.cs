using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
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

            migrationBuilder.AddColumn<ulong>(
                name: "discord_id",
                table: "discord_players",
                type: "INTEGER",
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
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ckey",
                table: "discord_players",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "discord_name",
                table: "discord_players",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_discord_players_ckey_discord_id",
                table: "discord_players",
                columns: new[] { "ckey", "discord_id" });
        }
    }
}
