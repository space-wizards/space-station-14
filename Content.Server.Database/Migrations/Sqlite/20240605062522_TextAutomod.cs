using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class TextAutomod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "censor_filter");

            migrationBuilder.CreateTable(
                name: "text_automod",
                columns: table => new
                {
                    filter_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pattern = table.Column<string>(type: "TEXT", nullable: false),
                    filter_type = table.Column<byte>(type: "INTEGER", nullable: false),
                    action_group = table.Column<string>(type: "TEXT", nullable: false),
                    target_flags = table.Column<int>(type: "INTEGER", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_text_automod", x => x.filter_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_text_automod_filter_id",
                table: "text_automod",
                column: "filter_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "text_automod");

            migrationBuilder.CreateTable(
                name: "censor_filter",
                columns: table => new
                {
                    censor_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    action_group = table.Column<string>(type: "TEXT", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false),
                    filter_type = table.Column<byte>(type: "INTEGER", nullable: false),
                    pattern = table.Column<string>(type: "TEXT", nullable: false),
                    target_flags = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_censor_filter", x => x.censor_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_censor_filter_censor_id",
                table: "censor_filter",
                column: "censor_id");
        }
    }
}
