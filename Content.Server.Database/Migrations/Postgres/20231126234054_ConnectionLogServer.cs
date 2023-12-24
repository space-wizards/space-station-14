using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ConnectionLogServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "connection_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Note: EF Core automatically makes indexes for all FKs.
            // That's really dumb, and there's no simple way to disable this.
            // So we drop the index creation command from the migration here,
            // as we don't want this index:

            // migrationBuilder.CreateIndex(
            //     name: "IX_connection_log_server_id",
            //     table: "connection_log",
            //     column: "server_id");

            migrationBuilder.AddForeignKey(
                name: "FK_connection_log_server_server_id",
                table: "connection_log",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_connection_log_server_server_id",
                table: "connection_log");

            // migrationBuilder.DropIndex(
            //     name: "IX_connection_log_server_id",
            //     table: "connection_log");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "connection_log");
        }
    }
}
