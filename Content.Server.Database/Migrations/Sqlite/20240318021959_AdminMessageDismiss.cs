using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdminMessageDismiss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "dismissed",
                table: "admin_messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE admin_messages SET dismissed = seen;");

            migrationBuilder.AddCheckConstraint(
                name: "NotDismissedAndSeen",
                table: "admin_messages",
                sql: "NOT dismissed OR seen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "NotDismissedAndSeen",
                table: "admin_messages");

            migrationBuilder.DropColumn(
                name: "dismissed",
                table: "admin_messages");
        }
    }
}
