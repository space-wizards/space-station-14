using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class Antags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Antag",
                columns: table => new
                {
                    AntagId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.CreateIndex(
                name: "IX_Antag_HumanoidProfileId_AntagName",
                table: "Antag",
                columns: new[] { "HumanoidProfileId", "AntagName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Antag");
        }
    }
}
