using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CosmaticDriftRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cdprofile",
                columns: table => new
                {
                    cdprofile_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    character_records = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cdprofile", x => x.cdprofile_id);
                    table.ForeignKey(
                        name: "FK_cdprofile_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cd_character_record_entries",
                columns: table => new
                {
                    cd_character_record_entries_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    involved = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<byte>(type: "smallint", nullable: false),
                    cdprofile_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_character_record_entries", x => x.cd_character_record_entries_id);
                    table.ForeignKey(
                        name: "FK_cd_character_record_entries_cdprofile_cdprofile_id",
                        column: x => x.cdprofile_id,
                        principalTable: "cdprofile",
                        principalColumn: "cdprofile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cd_character_record_entries_cd_character_record_entries_id",
                table: "cd_character_record_entries",
                column: "cd_character_record_entries_id");

            migrationBuilder.CreateIndex(
                name: "IX_cd_character_record_entries_cdprofile_id",
                table: "cd_character_record_entries",
                column: "cdprofile_id");

            migrationBuilder.CreateIndex(
                name: "IX_cdprofile_profile_id",
                table: "cdprofile",
                column: "profile_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cd_character_record_entries");

            migrationBuilder.DropTable(
                name: "cdprofile");
        }
    }
}
