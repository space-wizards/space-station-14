using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class HumanoidProfilePronouns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pronouns",
                columns: table => new
                {
                    pronouns_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    subject = table.Column<string>(type: "TEXT", nullable: true),
                    @object = table.Column<string>(name: "object", type: "TEXT", nullable: true),
                    dat_obj = table.Column<string>(type: "TEXT", nullable: true),
                    genitive = table.Column<string>(type: "TEXT", nullable: true),
                    poss_adj = table.Column<string>(type: "TEXT", nullable: true),
                    poss_pronoun = table.Column<string>(type: "TEXT", nullable: true),
                    reflexive = table.Column<string>(type: "TEXT", nullable: true),
                    counter = table.Column<string>(type: "TEXT", nullable: true),
                    plural = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pronouns", x => x.pronouns_id);
                    table.ForeignKey(
                        name: "FK_pronouns_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pronouns_profile_id",
                table: "pronouns",
                column: "profile_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pronouns");
        }
    }
}
