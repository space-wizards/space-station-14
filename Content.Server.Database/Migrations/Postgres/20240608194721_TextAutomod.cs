using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class TextAutomod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "text_automod",
                columns: table => new
                {
                    text_automod_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pattern = table.Column<string>(type: "text", nullable: false),
                    filter_type = table.Column<byte>(type: "smallint", nullable: false),
                    action_group = table.Column<string>(type: "text", nullable: false),
                    target_flags = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_text_automod", x => x.text_automod_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "text_automod");
        }
    }
}
