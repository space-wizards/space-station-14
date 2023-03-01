using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class Patrons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trait_profile_id",
                table: "trait");

            migrationBuilder.CreateTable(
                name: "patronlist",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patronlist", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "patron_item",
                columns: table => new
                {
                    patron_item_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    patron_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_class = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patron_item", x => x.patron_item_id);
                    table.ForeignKey(
                        name: "FK_patron_item_patronlist_patron_id",
                        column: x => x.patron_id,
                        principalTable: "patronlist",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trait_profile_id_trait_name",
                table: "trait",
                columns: new[] { "profile_id", "trait_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patron_item_patron_id_item_class",
                table: "patron_item",
                columns: new[] { "patron_id", "item_class" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patronlist_user_id",
                table: "patronlist",
                column: "user_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patron_item");

            migrationBuilder.DropTable(
                name: "patronlist");

            migrationBuilder.DropIndex(
                name: "IX_trait_profile_id_trait_name",
                table: "trait");

            migrationBuilder.CreateIndex(
                name: "IX_trait_profile_id",
                table: "trait",
                column: "profile_id");
        }
    }
}
