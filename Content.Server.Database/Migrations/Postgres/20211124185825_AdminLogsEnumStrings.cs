using System;
using Content.Shared.Administration.Logs;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class AdminLogsEnumStrings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "admin_log",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "impact",
                table: "admin_log",
                type: "text",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            foreach (var type in Enum.GetValues<LogType>())
            {
                migrationBuilder.UpdateData(
                    table: "admin_log",
                    keyColumn: "type",
                    keyValue: $"{(int) type}",
                    column: "type",
                    value: type.ToString());
            }

            foreach (var impact in Enum.GetValues<LogImpact>())
            {
                migrationBuilder.UpdateData(
                    table: "admin_log",
                    keyColumn: "impact",
                    keyValue: $"{(int) impact}",
                    column: "impact",
                    value: impact.ToString());
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "type",
                table: "admin_log",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<short>(
                name: "impact",
                table: "admin_log",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            foreach (var type in Enum.GetValues<LogType>())
            {
                migrationBuilder.UpdateData(
                    table: "admin_log",
                    keyColumn: "type",
                    keyValue: type.ToString(),
                    column: "type",
                    value: (int) type);
            }

            foreach (var impact in Enum.GetValues<LogImpact>())
            {
                migrationBuilder.UpdateData(
                    table: "admin_log",
                    keyColumn: "impact",
                    keyValue: impact.ToString(),
                    column: "impact",
                    value: (int) impact);
            }
        }
    }
}
