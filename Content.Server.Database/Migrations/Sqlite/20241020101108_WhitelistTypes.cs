using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class WhitelistTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_whitelist",
                table: "whitelist");

            migrationBuilder.AddColumn<string>(
                name: "whitelist_name",
                table: "whitelist",
                type: "TEXT",
                nullable: false,
                defaultValue: "DefaultWhitelist");

            migrationBuilder.AddPrimaryKey(
                name: "PK_whitelist",
                table: "whitelist",
                columns: new[] { "user_id", "whitelist_name" });

            migrationBuilder.CreateTable(
                name: "whitelist_type",
                columns: table => new
                {
                    whitelist_name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whitelist_type", x => x.whitelist_name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_whitelist_whitelist_name",
                table: "whitelist",
                column: "whitelist_name");

            migrationBuilder.InsertData(table: "whitelist_type", columns: new[] { "whitelist_name" }, values: ["DefaultWhitelist"]);

            migrationBuilder.AddForeignKey(
                name: "FK_whitelist_whitelist_type__whitelist_type_temp_id",
                table: "whitelist",
                column: "whitelist_name",
                principalTable: "whitelist_type",
                principalColumn: "whitelist_name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM whitelist WHERE whitelist_name != 'DefaultWhitelist'");

            migrationBuilder.DropForeignKey(
                name: "FK_whitelist_whitelist_type__whitelist_type_temp_id",
                table: "whitelist");

            migrationBuilder.DropTable(
                name: "whitelist_type");

            migrationBuilder.DropPrimaryKey(
                name: "PK_whitelist",
                table: "whitelist");

            migrationBuilder.DropIndex(
                name: "IX_whitelist_whitelist_name",
                table: "whitelist");

            migrationBuilder.DropColumn(
                name: "whitelist_name",
                table: "whitelist");

            migrationBuilder.AddPrimaryKey(
                name: "PK_whitelist",
                table: "whitelist",
                column: "user_id");
        }
    }
}
