using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class BanTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ban_template",
                columns: table => new
                {
                    ban_template_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    length = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    exempt_flags = table.Column<int>(type: "INTEGER", nullable: false),
                    severity = table.Column<int>(type: "INTEGER", nullable: false),
                    auto_delete = table.Column<bool>(type: "INTEGER", nullable: false),
                    hidden = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_template", x => x.ban_template_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ban_template");
        }
    }
}
