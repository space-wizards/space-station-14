using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class LoadoutExtraData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_extra_loadout_data_profile_role_loadout_id",
                table: "extra_loadout_data");

            migrationBuilder.CreateIndex(
                name: "IX_extra_loadout_data_profile_role_loadout_id",
                table: "extra_loadout_data",
                column: "profile_role_loadout_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_extra_loadout_data_profile_role_loadout_id",
                table: "extra_loadout_data");

            migrationBuilder.CreateIndex(
                name: "IX_extra_loadout_data_profile_role_loadout_id",
                table: "extra_loadout_data",
                column: "profile_role_loadout_id",
                unique: true);
        }
    }
}
