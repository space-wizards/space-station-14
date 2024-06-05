using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class CensorFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "censor_filter",
                columns: table => new
                {
                    censor_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pattern = table.Column<string>(type: "TEXT", nullable: false),
                    filter_type = table.Column<byte>(type: "INTEGER", nullable: false),
                    action_group = table.Column<string>(type: "TEXT", nullable: false),
                    target_flags = table.Column<int>(type: "INTEGER", nullable: false),
                    display_name = table.Column<string>(type: "TEXT", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "censor_filter");
        }
    }
}
