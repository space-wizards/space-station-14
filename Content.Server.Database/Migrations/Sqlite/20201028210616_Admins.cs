using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class Admins : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_rank",
                columns: table => new
                {
                    admin_rank_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_rank", x => x.admin_rank_id);
                });

            migrationBuilder.CreateTable(
                name: "admin",
                columns: table => new
                {
                    user_id = table.Column<Guid>(nullable: false),
                    title = table.Column<string>(nullable: true),
                    admin_rank_id = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_admin_admin_rank_admin_rank_id",
                        column: x => x.admin_rank_id,
                        principalTable: "admin_rank",
                        principalColumn: "admin_rank_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "admin_rank_flag",
                columns: table => new
                {
                    admin_rank_flag_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    flag = table.Column<string>(nullable: false),
                    admin_rank_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_rank_flag", x => x.admin_rank_flag_id);
                    table.ForeignKey(
                        name: "FK_admin_rank_flag_admin_rank_admin_rank_id",
                        column: x => x.admin_rank_id,
                        principalTable: "admin_rank",
                        principalColumn: "admin_rank_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_flag",
                columns: table => new
                {
                    admin_flag_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    flag = table.Column<string>(nullable: false),
                    negative = table.Column<bool>(nullable: false),
                    admin_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_flag", x => x.admin_flag_id);
                    table.ForeignKey(
                        name: "FK_admin_flag_admin_admin_id",
                        column: x => x.admin_id,
                        principalTable: "admin",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_admin_rank_id",
                table: "admin",
                column: "admin_rank_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_flag_admin_id",
                table: "admin_flag",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_rank_flag_admin_rank_id",
                table: "admin_rank_flag",
                column: "admin_rank_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_flag");

            migrationBuilder.DropTable(
                name: "admin_rank_flag");

            migrationBuilder.DropTable(
                name: "admin");

            migrationBuilder.DropTable(
                name: "admin_rank");
        }
    }
}
