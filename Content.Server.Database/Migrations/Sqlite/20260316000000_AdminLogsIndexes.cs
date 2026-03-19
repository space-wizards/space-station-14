using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdminLogsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop redundant standalone ServerId indexes (squashed from adminlogstest).
            migrationBuilder.DropIndex(
                name: "IX_admin_log_event_participant_server_id",
                table: "admin_log_event_participant");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_event_server_id",
                table: "admin_log_event");

            // Round-scoped impact index.
            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_round_id_impact_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "round_id", "impact", "occurred_at", "admin_log_event_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_log_event_server_id_round_id_impact_occurred_at_admin_log_event_id",
                table: "admin_log_event");

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id",
                table: "admin_log_event",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id",
                table: "admin_log_event_participant",
                column: "server_id");
        }
    }
}
