using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class UniqueHighPriorityJob : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_job_one_high_priority",
                table: "job",
                column: "profile_id",
                unique: true,
                filter: "priority = 3");

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_id_job_name",
                table: "job",
                columns: new[] { "profile_id", "job_name" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_one_high_priority",
                table: "job");

            migrationBuilder.DropIndex(
                name: "IX_job_profile_id_job_name",
                table: "job");
        }
    }
}
