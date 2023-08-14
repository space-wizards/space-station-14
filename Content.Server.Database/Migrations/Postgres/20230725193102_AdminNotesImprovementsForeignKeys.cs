using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AdminNotesImprovementsForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_messages_player_player_user_id",
                table: "admin_messages");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_player_user_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_watchlists_player_player_user_id",
                table: "admin_watchlists");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_messages_player_player_user_id",
                table: "admin_messages",
                column: "player_user_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_player_user_id",
                table: "admin_notes",
                column: "player_user_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_watchlists_player_player_user_id",
                table: "admin_watchlists",
                column: "player_user_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_messages_player_player_user_id",
                table: "admin_messages");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_player_user_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_watchlists_player_player_user_id",
                table: "admin_watchlists");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_messages_player_player_user_id",
                table: "admin_messages",
                column: "player_user_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_player_user_id",
                table: "admin_notes",
                column: "player_user_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_watchlists_player_player_user_id",
                table: "admin_watchlists",
                column: "player_user_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
