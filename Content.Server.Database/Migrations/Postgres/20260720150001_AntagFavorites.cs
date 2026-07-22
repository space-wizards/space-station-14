using Content.Server.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    // DS14-start
    [DbContext(typeof(PostgresServerDbContext))]
    [Migration("20260720150001_AntagFavorites")]
    public partial class AntagFavorites : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "favorite_antags",
                table: "preference",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
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
