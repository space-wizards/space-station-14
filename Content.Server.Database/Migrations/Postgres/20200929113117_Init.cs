using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assigned_user_id",
                columns: table => new
                {
                    assigned_user_id_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_name = table.Column<string>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_user_id", x => x.assigned_user_id_id);
                });

            migrationBuilder.CreateTable(
                name: "connection_log",
                columns: table => new
                {
                    connection_log_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(nullable: false),
                    user_name = table.Column<string>(nullable: false),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    address = table.Column<IPAddress>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connection_log", x => x.connection_log_id);
                    table.CheckConstraint("AddressNotIPv6MappedIPv4", "NOT inet '::ffff:0.0.0.0/96' >>= address");
                });

            migrationBuilder.CreateTable(
                name: "player",
                columns: table => new
                {
                    player_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(nullable: false),
                    first_seen_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_user_name = table.Column<string>(nullable: false),
                    last_seen_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_address = table.Column<IPAddress>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player", x => x.player_id);
                    table.CheckConstraint("LastSeenAddressNotIPv6MappedIPv4", "NOT inet '::ffff:0.0.0.0/96' >>= last_seen_address");
                });

            migrationBuilder.CreateTable(
                name: "preference",
                columns: table => new
                {
                    preference_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(nullable: false),
                    selected_character_slot = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preference", x => x.preference_id);
                });

            migrationBuilder.CreateTable(
                name: "server_ban",
                columns: table => new
                {
                    server_ban_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(nullable: true),
                    address = table.Column<ValueTuple<IPAddress, int>>(type: "inet", nullable: true),
                    ban_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(nullable: false),
                    banning_admin = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_ban", x => x.server_ban_id);
                    table.CheckConstraint("AddressNotIPv6MappedIPv4", "NOT inet '::ffff:0.0.0.0/96' >>= address");
                    table.CheckConstraint("HaveEitherAddressOrUserId", "address IS NOT NULL OR user_id IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    profile_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slot = table.Column<int>(nullable: false),
                    char_name = table.Column<string>(nullable: false),
                    age = table.Column<int>(nullable: false),
                    sex = table.Column<string>(nullable: false),
                    hair_name = table.Column<string>(nullable: false),
                    hair_color = table.Column<string>(nullable: false),
                    facial_hair_name = table.Column<string>(nullable: false),
                    facial_hair_color = table.Column<string>(nullable: false),
                    eye_color = table.Column<string>(nullable: false),
                    skin_color = table.Column<string>(nullable: false),
                    pref_unavailable = table.Column<int>(nullable: false),
                    preference_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_profile_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "server_unban",
                columns: table => new
                {
                    unban_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ban_id = table.Column<int>(nullable: false),
                    unbanning_admin = table.Column<Guid>(nullable: true),
                    unban_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "antag",
                columns: table => new
                {
                    antag_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(nullable: false),
                    antag_name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antag", x => x.antag_id);
                    table.ForeignKey(
                        name: "FK_antag_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job",
                columns: table => new
                {
                    job_id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(nullable: false),
                    job_name = table.Column<string>(nullable: false),
                    priority = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job", x => x.job_id);
                    table.ForeignKey(
                        name: "FK_job_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_antag_profile_id_antag_name",
                table: "antag",
                columns: new[] { "profile_id", "antag_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assigned_user_id_user_id",
                table: "assigned_user_id",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assigned_user_id_user_name",
                table: "assigned_user_id",
                column: "user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_connection_log_user_id",
                table: "connection_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_id",
                table: "job",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_user_id",
                table: "player",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_preference_user_id",
                table: "preference",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_preference_id",
                table: "profile",
                column: "preference_id");

            migrationBuilder.CreateIndex(
                name: "IX_profile_slot_preference_id",
                table: "profile",
                columns: new[] { "slot", "preference_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_address",
                table: "server_ban",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_user_id",
                table: "server_ban",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_unban_ban_id",
                table: "server_unban",
                column: "ban_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "antag");

            migrationBuilder.DropTable(
                name: "assigned_user_id");

            migrationBuilder.DropTable(
                name: "connection_log");

            migrationBuilder.DropTable(
                name: "job");

            migrationBuilder.DropTable(
                name: "player");

            migrationBuilder.DropTable(
                name: "server_unban");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "server_ban");

            migrationBuilder.DropTable(
                name: "preference");
        }
    }
}
