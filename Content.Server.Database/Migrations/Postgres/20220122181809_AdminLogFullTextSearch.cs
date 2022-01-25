using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class AdminLogFullTextSearch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_connection_log_server__server_id",
                table: "connection_log");

            migrationBuilder.DropForeignKey(
                name: "FK_round_server__server_id",
                table: "round");

            migrationBuilder.RenameIndex(
                name: "IX_round__server_id",
                table: "round",
                newName: "IX_round_server_id");

            migrationBuilder.AlterColumn<int>(
                name: "server_id",
                table: "round",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "server_id",
                table: "connection_log",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "admin_log",
                type: "tsvector",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_search_vector",
                table: "admin_log",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.AddForeignKey(
                name: "FK_connection_log_server__server_id",
                table: "connection_log",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id");

            migrationBuilder.AddForeignKey(
                name: "FK_round_server_server_id",
                table: "round",
                column: "server_id",
                principalTable: "server",
                principalColumn: "server_id");

            migrationBuilder.Sql(
                @"CREATE TRIGGER admin_log_search_vector_update BEFORE INSERT OR UPDATE
              ON admin_log FOR EACH ROW EXECUTE PROCEDURE
              tsvector_update_trigger(search_vector, 'pg_catalog.english', message);");

            migrationBuilder.Sql("UPDATE admin_log SET search_vector = to_tsvector('english', message)");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_connection_log_server__server_id",
                table: "connection_log");

            migrationBuilder.DropForeignKey(
                name: "FK_round_server_server_id",
                table: "round");

            migrationBuilder.DropIndex(
                name: "IX_admin_log_search_vector",
                table: "admin_log");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "admin_log");

            migrationBuilder.RenameIndex(
                name: "IX_round_server_id",
                table: "round",
                newName: "IX_round__server_id");

            migrationBuilder.AlterColumn<int>(
                name: "server_id",
                table: "round",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "server_id",
                table: "connection_log",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

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

            migrationBuilder.Sql("DROP TRIGGER admin_log_search_vector_update");
        }
    }
}
