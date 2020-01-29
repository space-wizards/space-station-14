using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations
{
    public partial class preferenceUnavailable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferenceUnavailable",
                table: "HumanoidProfile",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferenceUnavailable",
                table: "HumanoidProfile");
        }
    }
}
