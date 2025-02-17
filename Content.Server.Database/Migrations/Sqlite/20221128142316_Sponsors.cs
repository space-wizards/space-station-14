using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class Sponsors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sponsors",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tier = table.Column<int>(type: "INTEGER", nullable: false),
                    ooccolor = table.Column<string>(type: "TEXT", nullable: false),
                    have_priority_join = table.Column<bool>(type: "INTEGER", nullable: false),
                    allowed_markings = table.Column<string>(type: "TEXT", nullable: false),
                    extra_slots = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sponsors", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sponsors_user_id",
                table: "sponsors",
                column: "user_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sponsors");

        }
    }
}
