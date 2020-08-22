using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    PrefsId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(nullable: false),
                    SelectedCharacterSlot = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.PrefsId);
                });

            migrationBuilder.CreateTable(
                name: "HumanoidProfile",
                columns: table => new
                {
                    HumanoidProfileId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slot = table.Column<int>(nullable: false),
                    SlotName = table.Column<string>(nullable: false),
                    CharacterName = table.Column<string>(nullable: false),
                    Age = table.Column<int>(nullable: false),
                    Sex = table.Column<string>(nullable: false),
                    HairName = table.Column<string>(nullable: false),
                    HairColor = table.Column<string>(nullable: false),
                    FacialHairName = table.Column<string>(nullable: false),
                    FacialHairColor = table.Column<string>(nullable: false),
                    EyeColor = table.Column<string>(nullable: false),
                    SkinColor = table.Column<string>(nullable: false),
                    PrefsId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanoidProfile", x => x.HumanoidProfileId);
                    table.ForeignKey(
                        name: "FK_HumanoidProfile_Preferences_PrefsId",
                        column: x => x.PrefsId,
                        principalTable: "Preferences",
                        principalColumn: "PrefsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HumanoidProfile_PrefsId",
                table: "HumanoidProfile",
                column: "PrefsId");

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_Username",
                table: "Preferences",
                column: "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HumanoidProfile");

            migrationBuilder.DropTable(
                name: "Preferences");
        }
    }
}
