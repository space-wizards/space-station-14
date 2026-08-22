using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
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
                    flagged_word_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    word = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    flag_partial_matches = table.Column<bool>(type: "boolean", nullable: false),
                    severity = table.Column<byte>(type: "smallint", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
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
