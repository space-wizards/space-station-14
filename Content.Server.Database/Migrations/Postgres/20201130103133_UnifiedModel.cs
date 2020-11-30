using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class UnifiedModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This FK bypasses EFCore so need to handle it manually - 20kdc
            migrationBuilder.Sql(@"ALTER TABLE preference
DROP CONSTRAINT ""FK_preference_profile_selected_character_slot_preference_id""");

            migrationBuilder.DropCheckConstraint(
                name: "AddressNotIPv6MappedIPv4",
                table: "server_ban");

            migrationBuilder.DropCheckConstraint(
                name: "LastSeenAddressNotIPv6MappedIPv4",
                table: "player");

            migrationBuilder.DropColumn(
                name: "selected_character_slot",
                table: "preference");

            migrationBuilder.AlterColumn<DateTime>(
                name: "unban_time",
                table: "server_unban",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expiration_time",
                table: "server_ban",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ban_time",
                table: "server_ban",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_seen_time",
                table: "player",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "first_seen_time",
                table: "player",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "time",
                table: "connection_log",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "PreferenceProfile",
                columns: table => new
                {
                    PreferenceId = table.Column<int>(type: "integer", nullable: false),
                    ProfileId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenceProfile", x => new { x.PreferenceId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_PreferenceProfile_preference_PreferenceId",
                        column: x => x.PreferenceId,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PreferenceProfile_profile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreferenceProfile_PreferenceId",
                table: "PreferenceProfile",
                column: "PreferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreferenceProfile_ProfileId",
                table: "PreferenceProfile",
                column: "ProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferenceProfile");

            migrationBuilder.AlterColumn<DateTime>(
                name: "unban_time",
                table: "server_unban",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expiration_time",
                table: "server_ban",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ban_time",
                table: "server_ban",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<int>(
                name: "selected_character_slot",
                table: "preference",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_seen_time",
                table: "player",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "first_seen_time",
                table: "player",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "time",
                table: "connection_log",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddCheckConstraint(
                name: "AddressNotIPv6MappedIPv4",
                table: "server_ban",
                sql: "NOT inet '::ffff:0.0.0.0/96' >>= address");

            migrationBuilder.AddCheckConstraint(
                name: "LastSeenAddressNotIPv6MappedIPv4",
                table: "player",
                sql: "NOT inet '::ffff:0.0.0.0/96' >>= last_seen_address");

            migrationBuilder.Sql(@"ALTER TABLE preference
ADD CONSTRAINT ""FK_preference_profile_selected_character_slot_preference_id""
FOREIGN KEY (selected_character_slot, preference_id)
REFERENCES profile (slot, preference_id)
DEFERRABLE INITIALLY DEFERRED;");
        }
    }
}
