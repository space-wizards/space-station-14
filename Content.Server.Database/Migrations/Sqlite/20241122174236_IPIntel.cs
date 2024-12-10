using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class IPIntel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ipintel_cache",
                columns: table => new
                {
                    ipintel_cache_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    address = table.Column<string>(type: "TEXT", nullable: false),
                    time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    score = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ipintel_cache", x => x.ipintel_cache_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ipintel_cache_address",
                table: "ipintel_cache",
                column: "address",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ipintel_cache");
        }
    }
}
