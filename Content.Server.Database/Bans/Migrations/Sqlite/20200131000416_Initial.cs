using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Bans.Migrations.Sqlite
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IPBans",
                columns: table => new
                {
                    IPBanId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IpAddress = table.Column<string>(nullable: false),
                    Reason = table.Column<string>(nullable: false),
                    ExpiresOn = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPBans", x => x.IPBanId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IPBans");
        }
    }
}
