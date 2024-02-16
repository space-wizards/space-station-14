using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AdminNotesImprovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_created_by_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_deleted_by_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_last_edited_by_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_player_user_id",
                table: "admin_notes");

            migrationBuilder.DropCheckConstraint(
                name: "HaveEitherAddressOrUserIdOrHWId",
                table: "server_ban");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "server_role_ban",
                newName: "player_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_server_role_ban_user_id",
                table: "server_role_ban",
                newName: "IX_server_role_ban_player_user_id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "server_ban",
                newName: "player_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_server_ban_user_id",
                table: "server_ban",
                newName: "IX_server_ban_player_user_id");

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

            migrationBuilder.AddColumn<bool>(
                name: "hidden",
                table: "server_role_ban",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_edited_at",
                table: "server_role_ban",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_edited_by_id",
                table: "server_role_ban",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "playtime_at_note",
                table: "server_role_ban",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "round_id",
                table: "server_role_ban",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "severity",
                table: "server_role_ban",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "hidden",
                table: "server_ban",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_edited_at",
                table: "server_ban",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_edited_by_id",
                table: "server_ban",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "playtime_at_note",
                table: "server_ban",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "round_id",
                table: "server_ban",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "severity",
                table: "server_ban",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AlterColumn<Guid>(
                name: "player_user_id",
                table: "admin_notes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "last_edited_by_id",
                table: "admin_notes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_id",
                table: "admin_notes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "expiration_time",
                table: "admin_notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "severity",
                table: "admin_notes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "playtime_at_note",
                table: "admin_notes",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "admin_messages",
                columns: table => new
                {
                    admin_messages_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "interval", nullable: false),
                    message = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_edited_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    seen = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_messages", x => x.admin_messages_id);
                    table.ForeignKey(
                        name: "FK_admin_messages_player_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_messages_player_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_messages_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_messages_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_messages_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateTable(
                name: "admin_watchlists",
                columns: table => new
                {
                    admin_watchlists_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "interval", nullable: false),
                    message = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_edited_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_watchlists", x => x.admin_watchlists_id);
                    table.ForeignKey(
                        name: "FK_admin_watchlists_player_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_watchlists_player_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_watchlists_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_watchlists_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_watchlists_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_banning_admin",
                table: "server_role_ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_last_edited_by_id",
                table: "server_role_ban",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_role_ban_round_id",
                table: "server_role_ban",
                column: "round_id");

            migrationBuilder.AddCheckConstraint(
                name: "HaveEitherAddressOrUserIdOrHWId",
                table: "server_role_ban",
                sql: "address IS NOT NULL OR player_user_id IS NOT NULL OR hwid IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_banning_admin",
                table: "server_ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_last_edited_by_id",
                table: "server_ban",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_round_id",
                table: "server_ban",
                column: "round_id");

            migrationBuilder.AddCheckConstraint(
                name: "HaveEitherAddressOrUserIdOrHWId",
                table: "server_ban",
                sql: "address IS NOT NULL OR player_user_id IS NOT NULL OR hwid IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_admin_messages_created_by_id",
                table: "admin_messages",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_messages_deleted_by_id",
                table: "admin_messages",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_messages_last_edited_by_id",
                table: "admin_messages",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_messages_player_user_id",
                table: "admin_messages",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_messages_round_id",
                table: "admin_messages",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_watchlists_created_by_id",
                table: "admin_watchlists",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_watchlists_deleted_by_id",
                table: "admin_watchlists",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_watchlists_last_edited_by_id",
                table: "admin_watchlists",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_watchlists_player_user_id",
                table: "admin_watchlists",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_watchlists_round_id",
                table: "admin_watchlists",
                column: "round_id");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_created_by_id",
                table: "admin_notes",
                column: "created_by_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_deleted_by_id",
                table: "admin_notes",
                column: "deleted_by_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_last_edited_by_id",
                table: "admin_notes",
                column: "last_edited_by_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_player_user_id",
                table: "admin_notes",
                column: "player_user_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_server_ban_player_banning_admin",
                table: "server_ban",
                column: "banning_admin",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_server_ban_player_last_edited_by_id",
                table: "server_ban",
                column: "last_edited_by_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_server_ban_round_round_id",
                table: "server_ban",
                column: "round_id",
                principalTable: "round",
                principalColumn: "round_id");

            migrationBuilder.AddForeignKey(
                name: "FK_server_role_ban_player_banning_admin",
                table: "server_role_ban",
                column: "banning_admin",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_server_role_ban_player_last_edited_by_id",
                table: "server_role_ban",
                column: "last_edited_by_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_server_role_ban_round_round_id",
                table: "server_role_ban",
                column: "round_id",
                principalTable: "round",
                principalColumn: "round_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_created_by_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_deleted_by_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_last_edited_by_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_notes_player_player_user_id",
                table: "admin_notes");

            migrationBuilder.DropForeignKey(
                name: "FK_server_ban_player_banning_admin",
                table: "server_ban");

            migrationBuilder.DropForeignKey(
                name: "FK_server_ban_player_last_edited_by_id",
                table: "server_ban");

            migrationBuilder.DropForeignKey(
                name: "FK_server_ban_round_round_id",
                table: "server_ban");

            migrationBuilder.DropForeignKey(
                name: "FK_server_role_ban_player_banning_admin",
                table: "server_role_ban");

            migrationBuilder.DropForeignKey(
                name: "FK_server_role_ban_player_last_edited_by_id",
                table: "server_role_ban");

            migrationBuilder.DropForeignKey(
                name: "FK_server_role_ban_round_round_id",
                table: "server_role_ban");

            migrationBuilder.DropTable(
                name: "admin_messages");

            migrationBuilder.DropTable(
                name: "admin_watchlists");

            migrationBuilder.DropIndex(
                name: "IX_server_role_ban_banning_admin",
                table: "server_role_ban");

            migrationBuilder.DropIndex(
                name: "IX_server_role_ban_last_edited_by_id",
                table: "server_role_ban");

            migrationBuilder.DropIndex(
                name: "IX_server_role_ban_round_id",
                table: "server_role_ban");

            migrationBuilder.DropCheckConstraint(
                name: "HaveEitherAddressOrUserIdOrHWId",
                table: "server_role_ban");

            migrationBuilder.DropIndex(
                name: "IX_server_ban_banning_admin",
                table: "server_ban");

            migrationBuilder.DropIndex(
                name: "IX_server_ban_last_edited_by_id",
                table: "server_ban");

            migrationBuilder.DropIndex(
                name: "IX_server_ban_round_id",
                table: "server_ban");

            migrationBuilder.DropCheckConstraint(
                name: "HaveEitherAddressOrUserIdOrHWId",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "hidden",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "last_edited_at",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "last_edited_by_id",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "playtime_at_note",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "round_id",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "severity",
                table: "server_role_ban");

            migrationBuilder.DropColumn(
                name: "hidden",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "last_edited_at",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "last_edited_by_id",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "playtime_at_note",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "round_id",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "severity",
                table: "server_ban");

            migrationBuilder.DropColumn(
                name: "expiration_time",
                table: "admin_notes");

            migrationBuilder.DropColumn(
                name: "severity",
                table: "admin_notes");

            migrationBuilder.DropColumn(
                name: "playtime_at_note",
                table: "admin_notes");

            migrationBuilder.RenameColumn(
                name: "player_user_id",
                table: "server_role_ban",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_server_role_ban_player_user_id",
                table: "server_role_ban",
                newName: "IX_server_role_ban_user_id");

            migrationBuilder.RenameColumn(
                name: "player_user_id",
                table: "server_ban",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_server_ban_player_user_id",
                table: "server_ban",
                newName: "IX_server_ban_user_id");

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

            migrationBuilder.AlterColumn<Guid>(
                name: "player_user_id",
                table: "admin_notes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "last_edited_by_id",
                table: "admin_notes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_id",
                table: "admin_notes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "HaveEitherAddressOrUserIdOrHWId",
                table: "server_role_ban",
                sql: "address IS NOT NULL OR user_id IS NOT NULL OR hwid IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "HaveEitherAddressOrUserIdOrHWId",
                table: "server_ban",
                sql: "address IS NOT NULL OR user_id IS NOT NULL OR hwid IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_created_by_id",
                table: "admin_notes",
                column: "created_by_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_deleted_by_id",
                table: "admin_notes",
                column: "deleted_by_id",
                principalTable: "player",
                principalColumn: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_last_edited_by_id",
                table: "admin_notes",
                column: "last_edited_by_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_notes_player_player_user_id",
                table: "admin_notes",
                column: "player_user_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
