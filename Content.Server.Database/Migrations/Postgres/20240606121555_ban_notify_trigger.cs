using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ban_notify_trigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                create or replace function send_server_ban_notifcation()
                    returns trigger as $$
                    declare
                        x_server_id integer;
                    begin
                        select round.server_id into x_server_id from round where round.round_id = NEW.round_id;

                        perform pg_notify('ban_notification', json_build_object('player_id', NEW.player_user_id::text,'server_id', x_server_id)::text);
                        return new;
                    end;
                    $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                    create or replace trigger notify_on_server_ban_insert
                        after insert on server_ban
                        for each row
                        execute function send_server_ban_notifcation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                drop trigger notify_on_server_ban_insert on server_ban;
                drop function send_server_ban_notifcation;
            """);
        }
    }
}
