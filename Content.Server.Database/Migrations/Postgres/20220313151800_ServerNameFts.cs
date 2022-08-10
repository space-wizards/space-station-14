using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class ServerNameFts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "round",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    server_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server", x => x.server_id);
                });

            migrationBuilder.InsertData(
                "server",
                new[] {"server_id", "name"},
                new object[] { 0, "unknown" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_round_server_id",
                table: "round",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_message",
                table: "admin_log",
                column: "message")
                .Annotation("Npgsql:TsVectorConfig", "english");

            migrationBuilder.AddForeignKey(
                name: "FK_round_server_server_id",
                table: "round",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_round_server_server_id",
                table: "round");

            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.DropIndex(
                name: "IX_round_server_id",
                table: "round");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_message",
                table: "admin_log");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "round");
        }
    }
}
