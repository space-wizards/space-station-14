using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class AdminOocColor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "admin_ooc_color",
                table: "preference",
                type: "text",
                nullable: false,
                defaultValue: "#c43b23");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "admin_ooc_color",
                table: "preference");
        }
    }
}
