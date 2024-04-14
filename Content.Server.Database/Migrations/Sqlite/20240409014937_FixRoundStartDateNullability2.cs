using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class FixRoundStartDateNullability2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This needs to be its own separate migration,
            // because EF Core re-arranges the order of the commands if it's a single migration...
            // (only relevant for SQLite since it needs cursed shit to do ALTER COLUMN)
            migrationBuilder.Sql("UPDATE round SET start_date = NULL WHERE start_date = '0001-01-01 00:00:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
