using Content.Server.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    // DS14-start
    [DbContext(typeof(SqliteServerDbContext))]
    [Migration("20260720150000_AntagFavorites")]
    public partial class AntagFavorites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "favorite_antags",
                table: "preference",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "favorite_antags",
                table: "preference");
        }
    }
    // DS14-end
}
