using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class AdminNotesImprovement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trait_profile_id",
                table: "trait");

            migrationBuilder.RenameColumn(
                name: "shown_to_player",
                table: "admin_notes",
                newName: "secret");

            migrationBuilder.UpdateData(
                table: "admin_notes",
                keyColumn: "secret",
                keyValue: false,
                column: "secret",
                value: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expiry_time",
                table: "admin_notes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "note_severity",
                table: "admin_notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "note_type",
                table: "admin_notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "playtime_at_note",
                table: "admin_notes",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateIndex(
                name: "IX_trait_profile_id_trait_name",
                table: "trait",
                columns: new[] { "profile_id", "trait_name" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trait_profile_id_trait_name",
                table: "trait");

            migrationBuilder.DropColumn(
                name: "expiry_time",
                table: "admin_notes");

            migrationBuilder.DropColumn(
                name: "note_severity",
                table: "admin_notes");

            migrationBuilder.DropColumn(
                name: "note_type",
                table: "admin_notes");

            migrationBuilder.DropColumn(
                name: "playtime_at_note",
                table: "admin_notes");

            migrationBuilder.RenameColumn(
                name: "secret",
                table: "admin_notes",
                newName: "shown_to_player");

            migrationBuilder.UpdateData(
                table: "admin_notes",
                keyColumn: "shown_to_player",
                keyValue: true,
                column: "shown_to_player",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_trait_profile_id",
                table: "trait",
                column: "profile_id");
        }
    }
}
