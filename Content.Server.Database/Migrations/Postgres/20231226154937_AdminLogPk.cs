using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AdminLogPk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_log_player_admin_log_log_id_round_id",
                table: "admin_log_player");

            migrationBuilder.DropPrimaryKey(
                name: "PK_admin_log_player",
                table: "admin_log_player");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_player_log_id_round_id",
                table: "admin_log_player");

            migrationBuilder.DropPrimaryKey(
                name: "PK_admin_log",
                table: "admin_log");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_round_id",
                table: "admin_log");

            migrationBuilder.AddPrimaryKey(
                name: "PK_admin_log_player",
                table: "admin_log_player",
                columns: new[] { "round_id", "log_id", "player_user_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_admin_log",
                table: "admin_log",
                columns: new[] { "round_id", "admin_log_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_player_player_user_id",
                table: "admin_log_player",
                column: "player_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_log_player_admin_log_round_id_log_id",
                table: "admin_log_player",
                columns: new[] { "round_id", "log_id" },
                principalTable: "admin_log",
                principalColumns: new[] { "round_id", "admin_log_id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_log_player_admin_log_round_id_log_id",
                table: "admin_log_player");

            migrationBuilder.DropPrimaryKey(
                name: "PK_admin_log_player",
                table: "admin_log_player");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_player_player_user_id",
                table: "admin_log_player");

            migrationBuilder.DropPrimaryKey(
                name: "PK_admin_log",
                table: "admin_log");

            migrationBuilder.AddPrimaryKey(
                name: "PK_admin_log_player",
                table: "admin_log_player",
                columns: new[] { "player_user_id", "log_id", "round_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_admin_log",
                table: "admin_log",
                columns: new[] { "admin_log_id", "round_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_player_log_id_round_id",
                table: "admin_log_player",
                columns: new[] { "log_id", "round_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_round_id",
                table: "admin_log",
                column: "round_id");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_log_player_admin_log_log_id_round_id",
                table: "admin_log_player",
                columns: new[] { "log_id", "round_id" },
                principalTable: "admin_log",
                principalColumns: new[] { "admin_log_id", "round_id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
