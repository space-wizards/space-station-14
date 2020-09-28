using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssignedUserIds",
                columns: table => new
                {
                    AssignedUserIdId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedUserIds", x => x.AssignedUserIdId);
                });

            migrationBuilder.CreateTable(
                name: "Bans",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(nullable: true),
                    Address = table.Column<ValueTuple<IPAddress, int>>(type: "inet", nullable: true),
                    BanTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(nullable: false),
                    BanningAdmin = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bans", x => x.Id);
                    table.CheckConstraint("AddressNotIPv6MappedIPv4", "NOT inet '::ff:0.0.0.0/96' >>= \"Address\"");
                    table.CheckConstraint("HaveEitherAddressOrUserId", "\"Address\" IS NOT NULL OR \"UserId\" IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    PrefsId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(nullable: false),
                    SelectedCharacterSlot = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.PrefsId);
                });

            migrationBuilder.CreateTable(
                name: "Unbans",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BanId = table.Column<int>(nullable: false),
                    UnbanningAdmin = table.Column<Guid>(nullable: true),
                    UnbanTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unbans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unbans_Bans_BanId",
                        column: x => x.BanId,
                        principalTable: "Bans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    ProfileId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slot = table.Column<int>(nullable: false),
                    CharacterName = table.Column<string>(nullable: false),
                    Age = table.Column<int>(nullable: false),
                    Sex = table.Column<string>(nullable: false),
                    HairName = table.Column<string>(nullable: false),
                    HairColor = table.Column<string>(nullable: false),
                    FacialHairName = table.Column<string>(nullable: false),
                    FacialHairColor = table.Column<string>(nullable: false),
                    EyeColor = table.Column<string>(nullable: false),
                    SkinColor = table.Column<string>(nullable: false),
                    PreferenceUnavailable = table.Column<int>(nullable: false),
                    PrefsId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_Profiles_Preferences_PrefsId",
                        column: x => x.PrefsId,
                        principalTable: "Preferences",
                        principalColumn: "PrefsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Antag",
                columns: table => new
                {
                    AntagId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(nullable: false),
                    AntagName = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Antag", x => x.AntagId);
                    table.ForeignKey(
                        name: "FK_Antag_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Job",
                columns: table => new
                {
                    JobId = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(nullable: false),
                    JobName = table.Column<string>(nullable: false),
                    Priority = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_Job_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Antag_ProfileId_AntagName",
                table: "Antag",
                columns: new[] { "ProfileId", "AntagName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignedUserIds_UserId",
                table: "AssignedUserIds",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignedUserIds_UserName",
                table: "AssignedUserIds",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bans_Address",
                table: "Bans",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Bans_UserId",
                table: "Bans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_ProfileId",
                table: "Job",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_UserId",
                table: "Preferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_PrefsId",
                table: "Profiles",
                column: "PrefsId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Slot_PrefsId",
                table: "Profiles",
                columns: new[] { "Slot", "PrefsId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unbans_BanId",
                table: "Unbans",
                column: "BanId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Antag");

            migrationBuilder.DropTable(
                name: "AssignedUserIds");

            migrationBuilder.DropTable(
                name: "Job");

            migrationBuilder.DropTable(
                name: "Unbans");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Bans");

            migrationBuilder.DropTable(
                name: "Preferences");
        }
    }
}
