using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ModernHwid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "hwid_type",
                table: "server_role_ban",
                type: "INTEGER",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "hwid_type",
                table: "server_ban",
                type: "INTEGER",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "last_seen_hwid_type",
                table: "player",
                type: "INTEGER",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "hwid_type",
                table: "connection_log",
                type: "INTEGER",
                nullable: true,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hwid_type",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "hwid_type",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "last_seen_hwid_type",
                table: "player");

            migrationBuilder.DropColumn(
                name: "hwid_type",
                table: "connection_log");
        }
    }
}
