#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class DropAdminLogEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_log_entity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_log_entity",
                columns: table => new
                {
                    uid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admin_log_id = table.Column<int>(type: "integer", nullable: true),
                    admin_log_round_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_entity", x => x.uid);
                    table.ForeignKey(
                        name: "FK_admin_log_entity_admin_log_admin_log_id_admin_log_round_id",
                        columns: x => new { x.admin_log_id, x.admin_log_round_id },
                        principalTable: "admin_log",
                        principalColumns: new[] { "admin_log_id", "round_id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_entity_admin_log_id_admin_log_round_id",
                table: "admin_log_entity",
                columns: new[] { "admin_log_id", "admin_log_round_id" });
        }
    }
}
