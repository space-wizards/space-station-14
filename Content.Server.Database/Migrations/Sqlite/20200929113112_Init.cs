using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
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
                        .Annotation("Sqlite:Autoincrement", true),
                    user_name = table.Column<string>(nullable: false),
                    user_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_user_id", x => x.assigned_user_id_id);
                });

            migrationBuilder.CreateTable(
                name: "ban",
                columns: table => new
                {
                    ban_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(nullable: true),
                    address = table.Column<string>(nullable: true),
                    ban_time = table.Column<DateTime>(nullable: false),
                    expiration_time = table.Column<DateTime>(nullable: true),
                    reason = table.Column<string>(nullable: false),
                    banning_admin = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban", x => x.ban_id);
                });

            migrationBuilder.CreateTable(
                name: "connection_log",
                columns: table => new
                {
                    connection_log_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(nullable: false),
                    user_name = table.Column<string>(nullable: false),
                    time = table.Column<DateTime>(nullable: false),
                    address = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connection_log", x => x.connection_log_id);
                });

            migrationBuilder.CreateTable(
                name: "player",
                columns: table => new
                {
                    player_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(nullable: false),
                    first_seen_time = table.Column<DateTime>(nullable: false),
                    last_seen_user_name = table.Column<string>(nullable: false),
                    last_seen_time = table.Column<DateTime>(nullable: false),
                    last_seen_address = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player", x => x.player_id);
                });

            migrationBuilder.CreateTable(
                name: "preference",
                columns: table => new
                {
                    preference_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(nullable: false),
                    selected_character_slot = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preference", x => x.preference_id);
                });

            migrationBuilder.CreateTable(
                name: "unban",
                columns: table => new
                {
                    unban_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(nullable: false),
                    unbanning_admin = table.Column<Guid>(nullable: true),
                    unban_time = table.Column<DateTime>(nullable: false)
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

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    profile_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                name: "antag",
                columns: table => new
                {
                    antag_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                        .Annotation("Sqlite:Autoincrement", true),
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
                name: "IX_job_profile_id",
                table: "job",
                column: "profile_id");

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
                name: "IX_unban_ban_id",
                table: "unban",
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
                name: "unban");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "ban");

            migrationBuilder.DropTable(
                name: "preference");
        }
    }
}
