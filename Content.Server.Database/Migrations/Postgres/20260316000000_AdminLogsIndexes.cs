using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AdminLogsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop redundant standalone ServerId indexes — every composite index already
            // leads with ServerId, so these add write overhead with zero query benefit.
            migrationBuilder.DropIndex(
                name: "IX_admin_log_event_participant_server_id",
                table: "admin_log_event_participant");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_event_server_id",
                table: "admin_log_event");

            // Round-scoped impact index
            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_round_id_impact_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "round_id", "impact", "occurred_at", "admin_log_event_id" });

            // GIN tsvector index on payload message for fast full-text search.
            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_payload_message_gin",
                table: "admin_log_event_payload",
                column: "message")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_admin_log_event_server_id_round_id_impact_occurred_at_admin_log_event_id",
                table: "admin_log_event");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_event_payload_message_gin",
                table: "admin_log_event_payload");

            // Restore the standalone ServerId indexes that were dropped in Up().
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
