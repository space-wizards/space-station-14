using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class UsernameRuleNotifyTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                create or replace trigger notify_on_server_ban_insert
                    after update on server_username_rule
                    for each row
                    execute function send_server_username_rule_notification();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                drop trigger notify_on_server_ban_insert on server_username_rule;
                drop function send_server_username_rule_notification;
            """);
        }
    }
}
