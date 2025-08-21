using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RemoveDiscordIdAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_data_discord_id",
                table: "player_data");

            migrationBuilder.DropColumn(
                name: "discord_id",
                table: "player_data");

            migrationBuilder.DropColumn(
                name: "flags",
                table: "player_data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "discord_id",
                table: "player_data",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "flags",
                table: "player_data",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_player_data_discord_id",
                table: "player_data",
                column: "discord_id");
        }
    }
}
