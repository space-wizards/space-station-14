using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class Cleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_server_role_ban_server_role_unban__unban_id",
                table: "server_role_ban");

            migrationBuilder.DropForeignKey(
                name: "FK_server_role_unban_server_ban_ban_id",
                table: "server_role_unban");

            migrationBuilder.DropIndex(
                name: "IX_server_role_unban_ban_id",
                table: "server_role_unban");

            migrationBuilder.DropIndex(
                name: "IX_server_role_ban__unban_id",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "unban_id",
                table: "server_role_ban");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_unban_ban_id",
                table: "server_role_unban",
                column: "ban_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_address",
                table: "server_role_ban",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_user_id",
                table: "server_role_ban",
                column: "user_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_server_role_ban_HaveEitherAddressOrUserIdOrHWId",
                table: "server_role_ban",
                sql: "address IS NOT NULL OR user_id IS NOT NULL OR hwid IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_server_role_unban_server_role_ban_ban_id",
                table: "server_role_unban",
                column: "ban_id",
                principalTable: "server_role_ban",
                principalColumn: "server_role_ban_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_server_role_unban_server_role_ban_ban_id",
                table: "server_role_unban");

            migrationBuilder.DropIndex(
                name: "IX_server_role_unban_ban_id",
                table: "server_role_unban");

            migrationBuilder.DropIndex(
                name: "IX_server_role_ban_address",
                table: "server_role_ban");

            migrationBuilder.DropIndex(
                name: "IX_server_role_ban_user_id",
                table: "server_role_ban");

            migrationBuilder.DropCheckConstraint(
                name: "CK_server_role_ban_HaveEitherAddressOrUserIdOrHWId",
                table: "server_role_ban");

            migrationBuilder.AddColumn<int>(
                name: "unban_id",
                table: "server_role_ban",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_server_role_unban_ban_id",
                table: "server_role_unban",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban__unban_id",
                table: "server_role_ban",
                column: "unban_id");

            migrationBuilder.AddForeignKey(
                name: "FK_server_role_ban_server_role_unban__unban_id",
                table: "server_role_ban",
                column: "unban_id",
                principalTable: "server_role_unban",
                principalColumn: "role_unban_id");

            migrationBuilder.AddForeignKey(
                name: "FK_server_role_unban_server_ban_ban_id",
                table: "server_role_unban",
                column: "ban_id",
                principalTable: "server_ban",
                principalColumn: "server_ban_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
