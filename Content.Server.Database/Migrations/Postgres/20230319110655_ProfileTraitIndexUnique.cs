using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class ProfileTraitIndexUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trait_profile_id",
                table: "trait");

            migrationBuilder.CreateIndex(
                name: "IX_trait_profile_id_trait_name",
                table: "trait",
                columns: new[] { "profile_id", "trait_name" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
