using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class UsernameRuleInvariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ActiveRulesDoNotHaveRetireInformation",
                table: "server_username_rule",
                sql: "retired OR retire_time IS NULL AND retiring_admin IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "InactiveRulesHaveRetireInformation",
                table: "server_username_rule",
                sql: "NOT retired OR retire_time IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ActiveRulesDoNotHaveRetireInformation",
                table: "server_username_rule");

            migrationBuilder.DropCheckConstraint(
                name: "InactiveRulesHaveRetireInformation",
                table: "server_username_rule");
        }
    }
}
