using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class ServerNameRelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "server_id",
                table: "round",
                type: "integer",
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_round__server_id",
                table: "round",
                column: "server_id");

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
                name: "FK_round_server__server_id",
                table: "round");

            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.DropIndex(
                name: "IX_round__server_id",
                table: "round");

            migrationBuilder.DropColumn(
                name: "server_id",
                table: "round");
        }
    }
}
