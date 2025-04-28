using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class MultiCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_one_high_priority",
                table: "job");

            migrationBuilder.DropColumn(
                name: "pref_unavailable",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "selected_character_slot",
                table: "preference");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "job");

            migrationBuilder.AddColumn<bool>(
                name: "enabled",
                table: "profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "job_priority_entry",
                columns: table => new
                {
                    job_priority_entry_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    preference_id = table.Column<int>(type: "integer", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_priority_entry", x => x.job_priority_entry_id);
                    table.ForeignKey(
                        name: "FK_job_priority_entry_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_one_high_priority",
                table: "job_priority_entry",
                column: "preference_id",
                unique: true,
                filter: "priority = 3");

            migrationBuilder.CreateIndex(
                name: "IX_job_priority_entry_preference_id",
                table: "job_priority_entry",
                column: "preference_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_priority_entry");

            migrationBuilder.DropColumn(
                name: "enabled",
                table: "profile");

            migrationBuilder.AddColumn<int>(
                name: "pref_unavailable",
                table: "profile",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "selected_character_slot",
                table: "preference",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_job_one_high_priority",
                table: "job",
                column: "profile_id",
                unique: true,
                filter: "priority = 3");
        }
    }
}
