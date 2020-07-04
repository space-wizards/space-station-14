using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class UniqueAntags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Antag_HumanoidProfile_ProfileHumanoidProfileId",
                table: "Antag");

            migrationBuilder.DropIndex(
                name: "IX_Antag_ProfileHumanoidProfileId",
                table: "Antag");

            migrationBuilder.DropColumn(
                name: "ProfileHumanoidProfileId",
                table: "Antag");

            migrationBuilder.AddColumn<int>(
                name: "HumanoidProfileId",
                table: "Antag",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Antag_HumanoidProfileId_AntagName",
                table: "Antag",
                columns: new[] { "HumanoidProfileId", "AntagName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Antag_HumanoidProfile_HumanoidProfileId",
                table: "Antag",
                column: "HumanoidProfileId",
                principalTable: "HumanoidProfile",
                principalColumn: "HumanoidProfileId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Antag_HumanoidProfile_HumanoidProfileId",
                table: "Antag");

            migrationBuilder.DropIndex(
                name: "IX_Antag_HumanoidProfileId_AntagName",
                table: "Antag");

            migrationBuilder.DropColumn(
                name: "HumanoidProfileId",
                table: "Antag");

            migrationBuilder.AddColumn<int>(
                name: "ProfileHumanoidProfileId",
                table: "Antag",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Antag_ProfileHumanoidProfileId",
                table: "Antag",
                column: "ProfileHumanoidProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Antag_HumanoidProfile_ProfileHumanoidProfileId",
                table: "Antag",
                column: "ProfileHumanoidProfileId",
                principalTable: "HumanoidProfile",
                principalColumn: "HumanoidProfileId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
