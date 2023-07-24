using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UpdateBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "banning_admin_name",
                table: "server_ban",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "round",
                table: "server_ban",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "banning_admin_name",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "round",
                table: "server_ban");
        }
    }
}
