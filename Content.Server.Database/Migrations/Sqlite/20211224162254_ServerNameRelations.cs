using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class ServerNameRelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "round",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "connection_log",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    server_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server", x => x.server_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_round__server_id",
                table: "round",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_connection_log__server_id",
                table: "connection_log",
                column: "server_id");

            migrationBuilder.AddForeignKey(
                name: "FK_connection_log_server__server_id",
                table: "connection_log",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_round_server__server_id",
                table: "round",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_connection_log_server__server_id",
                table: "connection_log");

            migrationBuilder.DropForeignKey(
                name: "FK_round_server__server_id",
                table: "round");

            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.DropIndex(
                name: "IX_round__server_id",
                table: "round");

            migrationBuilder.DropIndex(
                name: "IX_connection_log__server_id",
                table: "connection_log");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "round");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "connection_log");
        }
    }
}
