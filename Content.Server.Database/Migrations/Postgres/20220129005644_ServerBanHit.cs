using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class ServerBanHit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "denied",
                table: "connection_log",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "server_ban_hit",
                columns: table => new
                {
                    server_ban_hit_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ban_id = table.Column<int>(type: "integer", nullable: false),
                    connection_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_ban_hit", x => x.server_ban_hit_id);
                    table.ForeignKey(
                        name: "FK_server_ban_hit_connection_log_connection_id",
                        column: x => x.connection_id,
                        principalTable: "connection_log",
                        principalColumn: "connection_log_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_server_ban_hit_server_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "server_ban",
                        principalColumn: "server_ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_hit_ban_id",
                table: "server_ban_hit",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_hit_connection_id",
                table: "server_ban_hit",
                column: "connection_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_ban_hit");

            migrationBuilder.DropColumn(
                name: "denied",
                table: "connection_log");
        }
    }
}
