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
            // Default to disabled
            migrationBuilder.AddColumn<bool>(
                name: "enabled",
                table: "profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Enable the currently selected character slot
            migrationBuilder.Sql(
                """
                UPDATE profile
                SET enabled = true
                WHERE EXISTS (
                    SELECT *
                    FROM preference
                    WHERE profile.preference_id = preference.preference_id
                      AND profile.slot = preference.selected_character_slot)
                """
            );

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

            migrationBuilder.Sql(
                """
                INSERT INTO job_priority_entry (preference_id, job_name, priority)
                SELECT prof.preference_id, job_name, priority from job
                INNER JOIN profile AS prof ON job.profile_id = prof.profile_id
                INNER JOIN preference AS pref ON pref.preference_id = prof.preference_id WHERE pref.selected_character_slot = prof.slot
                """);

            migrationBuilder.DropIndex(
                name: "IX_job_one_high_priority",
                table: "job");

            migrationBuilder.DropColumn(
                name: "selected_character_slot",
                table: "preference");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "job");

            migrationBuilder.DropColumn(
                name: "pref_unavailable",
                table: "profile");

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
            migrationBuilder.AddColumn<int>(
                name: "selected_character_slot",
                table: "preference",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE preference
                SET selected_character_slot =
                    (SELECT slot
                     FROM profile
                     WHERE profile.preference_id = preference.preference_id)
                WHERE TRUE
                """
            );

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
