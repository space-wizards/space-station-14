using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Preferences",
                table => new
                {
                    PrefsId = table.Column<int>()
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(),
                    SelectedCharacterSlot = table.Column<int>()
                },
                constraints: table => { table.PrimaryKey("PK_Preferences", x => x.PrefsId); });

            migrationBuilder.CreateTable(
                "HumanoidProfile",
                table => new
                {
                    HumanoidProfileId = table.Column<int>()
                        .Annotation("Sqlite:Autoincrement", true),
                    Slot = table.Column<int>(),
                    SlotName = table.Column<string>(),
                    CharacterName = table.Column<string>(),
                    Age = table.Column<int>(),
                    Sex = table.Column<string>(),
                    HairName = table.Column<string>(),
                    HairColor = table.Column<string>(),
                    FacialHairName = table.Column<string>(),
                    FacialHairColor = table.Column<string>(),
                    EyeColor = table.Column<string>(),
                    SkinColor = table.Column<string>(),
                    PrefsId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanoidProfile", x => x.HumanoidProfileId);
                    table.ForeignKey(
                        "FK_HumanoidProfile_Preferences_PrefsId",
                        x => x.PrefsId,
                        "Preferences",
                        "PrefsId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                "IX_HumanoidProfile_PrefsId",
                "HumanoidProfile",
                "PrefsId");

            migrationBuilder.CreateIndex(
                "IX_Preferences_Username",
                "Preferences",
                "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "HumanoidProfile");

            migrationBuilder.DropTable(
                "Preferences");
        }
    }
}
