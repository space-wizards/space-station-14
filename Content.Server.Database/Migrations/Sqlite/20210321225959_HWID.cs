using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class HWID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "last_seen_hwid",
                table: "player",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "hwid",
                table: "connection_log",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "hwid",
                table: "ban",
                type: "BLOB",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_seen_hwid",
                table: "player");

            migrationBuilder.DropColumn(
                name: "hwid",
                table: "connection_log");

            migrationBuilder.DropColumn(
                name: "hwid",
                table: "ban");
        }
    }
}
