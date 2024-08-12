#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RoleWhitelist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role_whitelists",
                columns: table => new
                {
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    role_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_whitelists", x => new { x.player_user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_role_whitelists_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_whitelists");
        }
    }
}
