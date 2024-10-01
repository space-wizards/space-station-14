using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UsernameRule : Migration
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
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_username_rule");
        }
    }
}
