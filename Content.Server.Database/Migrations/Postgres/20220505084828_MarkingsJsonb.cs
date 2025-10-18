#nullable disable

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class MarkingsJsonb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "markings",
                table: "profile");

            migrationBuilder.AddColumn<JsonDocument>(
                name: "markings",
                table: "profile",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "markings",
                table: "profile");

            migrationBuilder.AddColumn<string>(
                name: "markings",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
