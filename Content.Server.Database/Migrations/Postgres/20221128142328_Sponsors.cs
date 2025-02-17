using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class Sponsors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trait_profile_id",
                table: "trait");

            migrationBuilder.CreateTable(
                name: "sponsors",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    ooccolor = table.Column<string>(type: "text", nullable: false),
                    have_priority_join = table.Column<bool>(type: "boolean", nullable: false),
                    allowed_markings = table.Column<string>(type: "text", nullable: false),
                    extra_slots = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sponsors", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trait_profile_id_trait_name",
                table: "trait",
                columns: new[] { "profile_id", "trait_name" },
                unique: true);

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

            migrationBuilder.DropIndex(
                name: "IX_trait_profile_id_trait_name",
                table: "trait");

            migrationBuilder.CreateIndex(
                name: "IX_trait_profile_id",
                table: "trait",
                column: "profile_id");
        }
    }
}
