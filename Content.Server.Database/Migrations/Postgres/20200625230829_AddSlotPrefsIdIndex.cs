using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class AddSlotPrefsIdIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HumanoidProfile_Slot_PrefsId",
                table: "HumanoidProfile",
                columns: new[] { "Slot", "PrefsId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HumanoidProfile_Slot_PrefsId",
                table: "HumanoidProfile");
        }
    }
}
