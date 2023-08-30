using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class RoleBans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_role_unban",
                columns: table => new
                {
                    role_unban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    unban_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_role_unban", x => x.role_unban_id);
                    table.ForeignKey(
                        name: "FK_server_role_unban_server_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "server_ban",
                        principalColumn: "server_ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "server_role_ban",
                columns: table => new
                {
                    server_role_ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    hwid = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    banning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    unban_id = table.Column<int>(type: "INTEGER", nullable: true),
                    role_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_role_ban", x => x.server_role_ban_id);
                    table.ForeignKey(
                        name: "FK_server_role_ban_server_role_unban__unban_id",
                        column: x => x.unban_id,
                        principalTable: "server_role_unban",
                        principalColumn: "role_unban_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban__unban_id",
                table: "server_role_ban",
                column: "unban_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_unban_ban_id",
                table: "server_role_unban",
                column: "ban_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_role_ban");

            migrationBuilder.DropTable(
                name: "server_role_unban");
        }
    }
}
