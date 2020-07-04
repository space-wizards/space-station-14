using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Content.Server.Database.Migrations.Postgres
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
                    AntagId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AntagName = table.Column<string>(type: "text", nullable: false),
                    Preference = table.Column<bool>(type: "boolean", nullable: false),
                    ProfileHumanoidProfileId = table.Column<int>(type: "integer", nullable: false)
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
