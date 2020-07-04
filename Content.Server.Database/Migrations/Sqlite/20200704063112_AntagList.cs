using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class AntagList : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Antag");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Antag",
                columns: table => new
                {
                    AntagId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AntagName = table.Column<string>(type: "TEXT", nullable: false),
                    Preference = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProfileHumanoidProfileId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Antag", x => x.AntagId);
                    table.ForeignKey(
                        name: "FK_Antag_HumanoidProfile_ProfileHumanoidProfileId",
                        column: x => x.ProfileHumanoidProfileId,
                        principalTable: "HumanoidProfile",
                        principalColumn: "HumanoidProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Antag_ProfileHumanoidProfileId",
                table: "Antag",
                column: "ProfileHumanoidProfileId");
        }
    }
}
