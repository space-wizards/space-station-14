using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class RoleBans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_role_unban",
                columns: table => new
                {
                    role_unban_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ban_id = table.Column<int>(type: "integer", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "uuid", nullable: true),
                    unban_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_role_unban", x => x.role_unban_id);
                    table.ForeignKey(
                        name: "FK_server_role_unban_server_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "server_ban",
                        principalColumn: "server_ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "server_role_ban",
                columns: table => new
                {
                    server_role_ban_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    address = table.Column<ValueTuple<IPAddress, int>>(type: "inet", nullable: true),
                    hwid = table.Column<byte[]>(type: "bytea", nullable: true),
                    ban_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    banning_admin = table.Column<Guid>(type: "uuid", nullable: true),
                    unban_id = table.Column<int>(type: "integer", nullable: true),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_role_ban", x => x.server_role_ban_id);
                    table.CheckConstraint("CK_server_role_ban_AddressNotIPv6MappedIPv4", "NOT inet '::ffff:0.0.0.0/96' >>= address");
                    table.ForeignKey(
                        name: "FK_server_role_ban_server_role_unban__unban_id",
                        column: x => x.unban_id,
                        principalTable: "server_role_unban",
                        principalColumn: "role_unban_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban__unban_id",
                table: "server_role_ban",
                column: "unban_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_unban_ban_id",
                table: "server_role_unban",
                column: "ban_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_role_ban");

            migrationBuilder.DropTable(
                name: "server_role_unban");
        }
    }
}
