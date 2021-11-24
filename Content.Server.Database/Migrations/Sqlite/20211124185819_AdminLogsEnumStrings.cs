using System;
using Content.Shared.Administration.Logs;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class AdminLogsEnumStrings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "admin_log",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "impact",
                table: "admin_log",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "INTEGER");

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
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<sbyte>(
                name: "impact",
                table: "admin_log",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

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
