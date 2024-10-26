using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class UsernameBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_username_rule",
                columns: table => new
                {
                    server_username_rule_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    regex = table.Column<bool>(type: "boolean", nullable: false),
                    expression = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    restricting_admin = table.Column<Guid>(type: "uuid", nullable: true),
                    extend_to_ban = table.Column<bool>(type: "boolean", nullable: false),
                    retired = table.Column<bool>(type: "boolean", nullable: false),
                    retiring_admin = table.Column<Guid>(type: "uuid", nullable: true),
                    retire_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_username_rule", x => x.server_username_rule_id);
                    table.CheckConstraint("ActiveRulesDoNotHaveRetireInformation", "retired OR retire_time IS NULL AND retiring_admin IS NULL");
                    table.CheckConstraint("InactiveRulesHaveRetireInformation", "NOT retired OR retire_time IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "username_whitelist",
                columns: table => new
                {
                    username = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_username_whitelist", x => x.username);
                });

            migrationBuilder.Sql("""
                create or replace function send_server_username_rule_notification()
                    returns trigger ad $$
                    declare
                        x_server_id integer;
                    begin
                        select round.server_id into x_server_id from round where round.round.id = NEW.round_id;

                        perform pg_notify('username_rule_notification', json_build_object('username_rule_id', NEW.server_username_rule_id, 'server_id', x_server_id)::text);
                        return NEW;
                    end;
                    $$ LANGUAGE plpgsql;
            """);

            migrationBuilder.Sql("""
                create or replace trigger notify_on_server_username_ban_change
                    after insert OR update OR delete on server_username_rule
                    for each row
                    execute function send_server_username_rule_notification();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server_username_rule");

            migrationBuilder.DropTable(
                name: "username_whitelist");

            migrationBuilder.Sql("""
                drop trigger notify_on_server_username_ban_change on server_username_rule;
                drop function send_server_username_rule_notification;
            """);
        }
    }
}
