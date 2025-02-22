using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class server_asn_ban : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "server_asn_ban_id",
                table: "server_ban_hit",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "server_asn_ban",
                columns: table => new
                {
                    server_asn_ban_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asn = table.Column<string>(type: "text", nullable: false),
                    ban_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    banning_admin = table.Column<Guid>(type: "uuid", nullable: true),
                    last_edited_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    auto_delete = table.Column<bool>(type: "boolean", nullable: false),
                    hidden = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_asn_ban", x => x.server_asn_ban_id);
                    table.ForeignKey(
                        name: "FK_server_asn_ban_player_banning_admin",
                        column: x => x.banning_admin,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_asn_ban_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_hit_server_asn_ban_id",
                table: "server_ban_hit",
                column: "server_asn_ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_asn_ban_asn",
                table: "server_asn_ban",
                column: "asn");

            migrationBuilder.CreateIndex(
                name: "IX_server_asn_ban_banning_admin",
                table: "server_asn_ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_server_asn_ban_last_edited_by_id",
                table: "server_asn_ban",
                column: "last_edited_by_id");

            migrationBuilder.AddForeignKey(
                name: "FK_server_ban_hit_server_asn_ban_server_asn_ban_id",
                table: "server_ban_hit",
                column: "server_asn_ban_id",
                principalTable: "server_asn_ban",
                principalColumn: "server_asn_ban_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_server_ban_hit_server_asn_ban_server_asn_ban_id",
                table: "server_ban_hit");

            migrationBuilder.DropTable(
                name: "server_asn_ban");

            migrationBuilder.DropIndex(
                name: "IX_server_ban_hit_server_asn_ban_id",
                table: "server_ban_hit");

            migrationBuilder.DropColumn(
                name: "server_asn_ban_id",
                table: "server_ban_hit");
        }
    }
}
