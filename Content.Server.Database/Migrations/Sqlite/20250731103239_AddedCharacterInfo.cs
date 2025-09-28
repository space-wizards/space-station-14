using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddedCharacterInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sl_character_info",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    physical_desc = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    personality_desc = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    personal_notes = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    character_secrets = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    exploitable_info = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    oocnotes = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sl_character_info", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_sl_character_info_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sl_character_info");
        }
    }
}