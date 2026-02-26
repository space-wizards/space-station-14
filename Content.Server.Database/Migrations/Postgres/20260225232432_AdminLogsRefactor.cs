using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AdminLogsRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_log_participant_entity",
                columns: table => new
                {
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    log_id = table.Column<int>(type: "integer", nullable: false),
                    entity_uid = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<byte>(type: "smallint", nullable: false),
                    prototype_id = table.Column<string>(type: "text", nullable: true),
                    entity_name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_participant_entity", x => new { x.round_id, x.log_id, x.entity_uid, x.role });
                    table.ForeignKey(
                        name: "FK_admin_log_participant_entity_admin_log_round_id_log_id",
                        columns: x => new { x.round_id, x.log_id },
                        principalTable: "admin_log",
                        principalColumns: new[] { "round_id", "admin_log_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_type_impact_date",
                table: "admin_log",
                columns: new[] { "type", "impact", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_participant_entity_entity_uid_role_round_id_log_id",
                table: "admin_log_participant_entity",
                columns: new[] { "entity_uid", "role", "round_id", "log_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_participant_entity_entity_uid_round_id_log_id",
                table: "admin_log_participant_entity",
                columns: new[] { "entity_uid", "round_id", "log_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_log_participant_entity");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_type_impact_date",
                table: "admin_log");
        }
    }
}
