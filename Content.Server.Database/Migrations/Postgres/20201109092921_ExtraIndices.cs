using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class ExtraIndices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_player_last_seen_user_name",
                table: "player",
                column: "last_seen_user_name");

            migrationBuilder.CreateIndex(
                name: "IX_admin_rank_flag_flag_admin_rank_id",
                table: "admin_rank_flag",
                columns: new[] { "flag", "admin_rank_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_flag_flag_admin_id",
                table: "admin_flag",
                columns: new[] { "flag", "admin_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_player_last_seen_user_name",
                table: "player");

            migrationBuilder.DropIndex(
                name: "IX_admin_rank_flag_flag_admin_rank_id",
                table: "admin_rank_flag");

            migrationBuilder.DropIndex(
                name: "IX_admin_flag_flag_admin_id",
                table: "admin_flag");
        }
    }
}
