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
                create function send_server_ban_notifcation()
                    returns trigger as $$
                    begin
                        perform pg_notify('ban_notification', NEW.player_user_id::text);
                        return new;
                    end;
                    $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                    create trigger notify_on_server_ban_insert
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
