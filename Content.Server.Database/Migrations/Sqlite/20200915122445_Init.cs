using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    PrefsId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(nullable: false),
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
                    PreferenceUnavailable = table.Column<int>(nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Antag",
                columns: table => new
                {
                    AntagId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HumanoidProfileId = table.Column<int>(nullable: false),
                    AntagName = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Antag", x => x.AntagId);
                    table.ForeignKey(
                        name: "FK_Antag_HumanoidProfile_HumanoidProfileId",
                        column: x => x.HumanoidProfileId,
                        principalTable: "HumanoidProfile",
                        principalColumn: "HumanoidProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Job",
                columns: table => new
                {
                    JobId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileHumanoidProfileId = table.Column<int>(nullable: false),
                    JobName = table.Column<string>(nullable: false),
                    Priority = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_Job_HumanoidProfile_ProfileHumanoidProfileId",
                        column: x => x.ProfileHumanoidProfileId,
                        principalTable: "HumanoidProfile",
                        principalColumn: "HumanoidProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Antag_HumanoidProfileId_AntagName",
                table: "Antag",
                columns: new[] { "HumanoidProfileId", "AntagName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HumanoidProfile_PrefsId",
                table: "HumanoidProfile",
                column: "PrefsId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanoidProfile_Slot_PrefsId",
                table: "HumanoidProfile",
                columns: new[] { "Slot", "PrefsId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Job_ProfileHumanoidProfileId",
                table: "Job",
                column: "ProfileHumanoidProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId",
                table: "Preferences",
                column: "UserId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Antag");

            migrationBuilder.DropTable(
                name: "Job");

            migrationBuilder.DropTable(
                name: "HumanoidProfile");

            migrationBuilder.DropTable(
                name: "Preferences");
        }
    }
}
