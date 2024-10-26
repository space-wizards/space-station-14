using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UsernameBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_username_rule",
                columns: table => new
                {
                    server_username_rule_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    creation_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    regex = table.Column<bool>(type: "INTEGER", nullable: false),
                    expression = table.Column<string>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    restricting_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    extend_to_ban = table.Column<bool>(type: "INTEGER", nullable: false),
                    retired = table.Column<bool>(type: "INTEGER", nullable: false),
                    retiring_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    retire_time = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_username_rule", x => x.server_username_rule_id);
                    table.CheckConstraint("ActiveRulesDoNotHaveRetireInformation", "retired OR retire_time IS NULL AND retiring_admin IS NULL");
                    table.CheckConstraint("InactiveRulesHaveRetireInformation", "NOT retired OR retire_time IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "username_whitelist",
                columns: table => new
                {
                    username = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_username_whitelist", x => x.username);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_username_rule");

            migrationBuilder.DropTable(
                name: "username_whitelist");
        }
    }
}
