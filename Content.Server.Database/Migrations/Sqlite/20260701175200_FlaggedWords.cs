using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class FlaggedWords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "flagged_word",
                columns: table => new
                {
                    flagged_word_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    word = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    flag_partial_matches = table.Column<bool>(type: "INTEGER", nullable: false),
                    severity = table.Column<byte>(type: "INTEGER", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flagged_word", x => x.flagged_word_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_flagged_word_word",
                table: "flagged_word",
                column: "word",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flagged_word");
        }
    }
}
