using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class BanRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_server_ban_hit_server_ban_ban_id",
                table: "server_ban_hit");

            migrationBuilder.DropTable(
                name: "server_role_unban");

            migrationBuilder.DropTable(
                name: "server_unban");

            migrationBuilder.DropTable(
                name: "server_role_ban");

            migrationBuilder.DropTable(
                name: "server_ban");

            migrationBuilder.CreateTable(
                name: "ban",
                columns: table => new
                {
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    type = table.Column<byte>(type: "INTEGER", nullable: false),
                    playtime_at_note = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    severity = table.Column<int>(type: "INTEGER", nullable: false),
                    banning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    exempt_flags = table.Column<int>(type: "INTEGER", nullable: false),
                    auto_delete = table.Column<bool>(type: "INTEGER", nullable: false),
                    hidden = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban", x => x.ban_id);
                    table.CheckConstraint("NoExemptOnRoleBan", "type = 0 OR exempt_flags = 0");
                    table.ForeignKey(
                        name: "FK_ban_player_banning_admin",
                        column: x => x.banning_admin,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ban_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ban_address",
                columns: table => new
                {
                    ban_address_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    address = table.Column<string>(type: "TEXT", nullable: false),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_address", x => x.ban_address_id);
                    table.ForeignKey(
                        name: "FK_ban_address_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban_hwid",
                columns: table => new
                {
                    ban_hwid_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    hwid = table.Column<byte[]>(type: "BLOB", nullable: false),
                    hwid_type = table.Column<int>(type: "INTEGER", nullable: false),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_hwid", x => x.ban_hwid_id);
                    table.ForeignKey(
                        name: "FK_ban_hwid_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban_player",
                columns: table => new
                {
                    ban_player_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_player", x => x.ban_player_id);
                    table.ForeignKey(
                        name: "FK_ban_player_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban_role",
                columns: table => new
                {
                    ban_role_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    role_type = table.Column<string>(type: "TEXT", nullable: false),
                    role_id = table.Column<string>(type: "TEXT", nullable: false),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_role", x => x.ban_role_id);
                    table.ForeignKey(
                        name: "FK_ban_role_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban_round",
                columns: table => new
                {
                    ban_round_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_round", x => x.ban_round_id);
                    table.ForeignKey(
                        name: "FK_ban_round_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ban_round_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unban",
                columns: table => new
                {
                    unban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    unban_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unban", x => x.unban_id);
                    table.ForeignKey(
                        name: "FK_unban_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ban_banning_admin",
                table: "ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_ban_last_edited_by_id",
                table: "ban",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_address_ban_id",
                table: "ban_address",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_hwid_ban_id",
                table: "ban_hwid",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_player_ban_id",
                table: "ban_player",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_player_user_id_ban_id",
                table: "ban_player",
                columns: new[] { "user_id", "ban_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ban_role_ban_id",
                table: "ban_role",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_role_role_type_role_id_ban_id",
                table: "ban_role",
                columns: new[] { "role_type", "role_id", "ban_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ban_round_ban_id",
                table: "ban_round",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_round_round_id_ban_id",
                table: "ban_round",
                columns: new[] { "round_id", "ban_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unban_ban_id",
                table: "unban",
                column: "ban_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_server_ban_hit_ban_ban_id",
                table: "server_ban_hit",
                column: "ban_id",
                principalTable: "ban",
                principalColumn: "ban_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_server_ban_hit_ban_ban_id",
                table: "server_ban_hit");

            migrationBuilder.DropTable(
                name: "ban_address");

            migrationBuilder.DropTable(
                name: "ban_hwid");

            migrationBuilder.DropTable(
                name: "ban_player");

            migrationBuilder.DropTable(
                name: "ban_role");

            migrationBuilder.DropTable(
                name: "ban_round");

            migrationBuilder.DropTable(
                name: "unban");

            migrationBuilder.DropTable(
                name: "ban");

            migrationBuilder.CreateTable(
                name: "server_ban",
                columns: table => new
                {
                    server_ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    banning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    auto_delete = table.Column<bool>(type: "INTEGER", nullable: false),
                    ban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    exempt_flags = table.Column<int>(type: "INTEGER", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    hidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    severity = table.Column<int>(type: "INTEGER", nullable: false),
                    hwid = table.Column<byte[]>(type: "BLOB", nullable: true),
                    hwid_type = table.Column<int>(type: "INTEGER", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_ban", x => x.server_ban_id);
                    table.CheckConstraint("HaveEitherAddressOrUserIdOrHWId", "address IS NOT NULL OR player_user_id IS NOT NULL OR hwid IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_server_ban_player_banning_admin",
                        column: x => x.banning_admin,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_ban_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_ban_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateTable(
                name: "server_role_ban",
                columns: table => new
                {
                    server_role_ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    banning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    ban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    hidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    role_id = table.Column<string>(type: "TEXT", nullable: false),
                    severity = table.Column<int>(type: "INTEGER", nullable: false),
                    hwid = table.Column<byte[]>(type: "BLOB", nullable: true),
                    hwid_type = table.Column<int>(type: "INTEGER", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_role_ban", x => x.server_role_ban_id);
                    table.CheckConstraint("HaveEitherAddressOrUserIdOrHWId", "address IS NOT NULL OR player_user_id IS NOT NULL OR hwid IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_server_role_ban_player_banning_admin",
                        column: x => x.banning_admin,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_role_ban_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_server_role_ban_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateTable(
                name: "server_unban",
                columns: table => new
                {
                    unban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_unban", x => x.unban_id);
                    table.ForeignKey(
                        name: "FK_server_unban_server_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "server_ban",
                        principalColumn: "server_ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "server_role_unban",
                columns: table => new
                {
                    role_unban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_role_unban", x => x.role_unban_id);
                    table.ForeignKey(
                        name: "FK_server_role_unban_server_role_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "server_role_ban",
                        principalColumn: "server_role_ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_address",
                table: "server_ban",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_banning_admin",
                table: "server_ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_last_edited_by_id",
                table: "server_ban",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_player_user_id",
                table: "server_ban",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_round_id",
                table: "server_ban",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_address",
                table: "server_role_ban",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_banning_admin",
                table: "server_role_ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_last_edited_by_id",
                table: "server_role_ban",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_player_user_id",
                table: "server_role_ban",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_round_id",
                table: "server_role_ban",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_unban_ban_id",
                table: "server_role_unban",
                column: "ban_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_server_unban_ban_id",
                table: "server_unban",
                column: "ban_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_server_ban_hit_server_ban_ban_id",
                table: "server_ban_hit",
                column: "ban_id",
                principalTable: "server_ban",
                principalColumn: "server_ban_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
