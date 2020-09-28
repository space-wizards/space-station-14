﻿using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class bans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bans",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(nullable: true),
                    Address = table.Column<ValueTuple<IPAddress, int>>(type: "inet", nullable: true),
                    BanTime = table.Column<DateTimeOffset>(nullable: false),
                    ExpirationTime = table.Column<DateTimeOffset>(nullable: true),
                    Reason = table.Column<string>(nullable: false),
                    BanningAdmin = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bans", x => x.Id);
                    table.CheckConstraint("AddressIsIPv6", "family(\"Address\") = 6");
                    table.CheckConstraint("HaveEitherAddressOrUserId", "\"Address\" IS NOT NULL OR \"UserId\" IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "Unbans",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BanId = table.Column<int>(nullable: false),
                    UnbanningAdmin = table.Column<Guid>(nullable: false),
                    UnbanTime = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unbans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unbans_Bans_BanId",
                        column: x => x.BanId,
                        principalTable: "Bans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bans_Address",
                table: "Bans",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Bans_UserId",
                table: "Bans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Unbans_BanId",
                table: "Unbans",
                column: "BanId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Unbans");

            migrationBuilder.DropTable(
                name: "Bans");
        }
    }
}
