using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class BanRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ban",
                columns: table => new
                {
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    type = table.Column<byte>(type: "INTEGER", nullable: false),
                    playtime_at_note = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ban_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    severity = table.Column<int>(type: "INTEGER", nullable: false),
                    banning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    exempt_flags = table.Column<int>(type: "INTEGER", nullable: false),
                    auto_delete = table.Column<bool>(type: "INTEGER", nullable: false),
                    hidden = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban", x => x.ban_id);
                    table.CheckConstraint("NoExemptOnRoleBan", "type = 0 OR exempt_flags = 0");
                    table.ForeignKey(
                        name: "FK_ban_player_banning_admin",
                        column: x => x.banning_admin,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ban_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ban_address",
                columns: table => new
                {
                    ban_address_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    address = table.Column<string>(type: "TEXT", nullable: false),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_address", x => x.ban_address_id);
                    table.ForeignKey(
                        name: "FK_ban_address_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban_hwid",
                columns: table => new
                {
                    ban_hwid_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    hwid = table.Column<byte[]>(type: "BLOB", nullable: false),
                    hwid_type = table.Column<int>(type: "INTEGER", nullable: false),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_hwid", x => x.ban_hwid_id);
                    table.ForeignKey(
                        name: "FK_ban_hwid_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban_player",
                columns: table => new
                {
                    ban_player_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_player", x => x.ban_player_id);
                    table.ForeignKey(
                        name: "FK_ban_player_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban_role",
                columns: table => new
                {
                    ban_role_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    role_type = table.Column<string>(type: "TEXT", nullable: false),
                    role_id = table.Column<string>(type: "TEXT", nullable: false),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_role", x => x.ban_role_id);
                    table.ForeignKey(
                        name: "FK_ban_role_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban_round",
                columns: table => new
                {
                    ban_round_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_round", x => x.ban_round_id);
                    table.ForeignKey(
                        name: "FK_ban_round_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ban_round_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unban",
                columns: table => new
                {
                    unban_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    unbanning_admin = table.Column<Guid>(type: "TEXT", nullable: true),
                    unban_time = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unban", x => x.unban_id);
                    table.ForeignKey(
                        name: "FK_unban_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ban_banning_admin",
                table: "ban",
                column: "banning_admin");

            migrationBuilder.CreateIndex(
                name: "IX_ban_last_edited_by_id",
                table: "ban",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_address_ban_id",
                table: "ban_address",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_hwid_ban_id",
                table: "ban_hwid",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_player_ban_id",
                table: "ban_player",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_player_user_id_ban_id",
                table: "ban_player",
                columns: new[] { "user_id", "ban_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ban_role_ban_id",
                table: "ban_role",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_role_role_type_role_id_ban_id",
                table: "ban_role",
                columns: new[] { "role_type", "role_id", "ban_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ban_round_ban_id",
                table: "ban_round",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_ban_round_round_id_ban_id",
                table: "ban_round",
                columns: new[] { "round_id", "ban_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unban_ban_id",
                table: "unban",
                column: "ban_id",
                unique: true);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_ban_hwid_hwid_ban_id"
                    ON ban_hwid
                    (hwid_type, hwid, ban_id);

                CREATE UNIQUE INDEX "IX_ban_address_address_ban_id"
                    ON ban_address
                    (address, ban_id);
                """);

            migrationBuilder.Sql("""
                --
                -- Insert game bans
                --
                INSERT INTO
                	ban	(ban_id, type, playtime_at_note, ban_time, expiration_time, reason, severity, banning_admin, last_edited_by_id, last_edited_at, exempt_flags, auto_delete, hidden)
                SELECT
                	server_ban_id, 0, playtime_at_note, ban_time, expiration_time, reason, severity, banning_admin, last_edited_by_id, last_edited_at, exempt_flags, auto_delete, hidden
                FROM
                	server_ban;

                -- Insert ban player records.
                INSERT INTO
                	ban_player (user_id, ban_id)
                SELECT
                	player_user_id, server_ban_id
                FROM
                	server_ban
                WHERE
                	player_user_id IS NOT NULL;

                -- Insert ban address records.
                INSERT INTO
                	ban_address (address, ban_id)
                SELECT
                	address, server_ban_id
                FROM
                	server_ban
                WHERE
                	address IS NOT NULL;

                -- Insert ban HWID records.
                INSERT INTO
                	ban_hwid (hwid, hwid_type, ban_id)
                SELECT
                	hwid, hwid_type, server_ban_id
                FROM
                	server_ban
                WHERE
                	hwid IS NOT NULL;

                -- Insert ban unban records.
                INSERT INTO
                	unban (ban_id, unbanning_admin, unban_time)
                SELECT
                	ban_id, unbanning_admin, unban_time
                FROM server_unban;

                -- Insert ban round records.
                INSERT INTO
                	ban_round (round_id, ban_id)
                SELECT
                	round_id, server_ban_id
                FROM
                	server_ban
                WHERE
                	round_id IS NOT NULL;

                --
                -- Insert role bans
                -- This shit is a pain in the ass
                -- > Declarative language
                -- > Has to write procedural code in it
                --

                -- Create mapping table from role ban -> server ban.
                -- We have to manually calculate the new ban IDs by using the sequence.
                -- We also want to merge role ban records because the game code previously did that in some UI,
                -- and that code is now gone, expecting the DB to do it.

                -- Create a table to store IDs to merge.
                CREATE TEMPORARY TABLE _role_ban_import_merge_map (merge_id INTEGER, server_role_ban_id INTEGER UNIQUE);

                -- Create a table to store merged IDs -> new ban IDs
                CREATE TEMPORARY TABLE _role_ban_import_id_map (ban_id INTEGER UNIQUE, merge_id INTEGER UNIQUE);

                -- Calculate merged role bans.
                INSERT INTO
                	_role_ban_import_merge_map
                SELECT
                	(
                		SELECT
                			sub.server_role_ban_id
                		FROM
                			server_role_ban AS sub
                		LEFT JOIN server_role_unban AS sub_unban
                		ON sub_unban.ban_id = sub.server_role_ban_id
                		WHERE
                			main.reason IS NOT DISTINCT FROM sub.reason
                			AND main.player_user_id IS NOT DISTINCT FROM sub.player_user_id
                			AND main.address IS NOT DISTINCT FROM sub.address
                			AND main.hwid IS NOT DISTINCT FROM sub.hwid
                			AND main.hwid_type IS NOT DISTINCT FROM sub.hwid_type
                			AND main.ban_time = sub.ban_time
                			AND (
                				(main.expiration_time IS NULL) = (sub.expiration_time IS NULL)
                				OR main.expiration_time = sub.expiration_time
                			)
                			AND main.round_id IS NOT DISTINCT FROM sub.round_id
                			AND main.severity IS NOT DISTINCT FROM sub.severity
                			AND main.hidden IS NOT DISTINCT FROM sub.hidden
                			AND main.banning_admin IS NOT DISTINCT FROM sub.banning_admin
                			AND (sub_unban.ban_id IS NULL) = (main_unban.ban_id IS NULL)
                		ORDER BY
                			sub.server_role_ban_id ASC
                		LIMIT 1
                	), main.server_role_ban_id
                FROM
                	server_role_ban AS main
                LEFT JOIN server_role_unban AS main_unban
                ON main_unban.ban_id = main.server_role_ban_id;

                -- Assign new ban IDs for merged IDs.
                INSERT OR IGNORE INTO
                	_role_ban_import_id_map
                SELECT
                	merge_id + (SELECT seq FROM sqlite_sequence WHERE name = 'ban'),
                	merge_id
                FROM
                	_role_ban_import_merge_map;

                -- I sure fucking wish CTEs could span multiple queries...

                -- Insert new ban records
                INSERT INTO
                	ban	(ban_id, type, playtime_at_note, ban_time, expiration_time, reason, severity, banning_admin, last_edited_by_id, last_edited_at, exempt_flags, auto_delete, hidden)
                SELECT
                	im.ban_id, 1, playtime_at_note, ban_time, expiration_time, reason, severity, banning_admin, last_edited_by_id, last_edited_at, 0, FALSE, hidden
                FROM
                	_role_ban_import_id_map im
                INNER JOIN _role_ban_import_merge_map mm
                ON im.merge_id = mm.merge_id
                INNER JOIN server_role_ban srb
                ON srb.server_role_ban_id = im.merge_id
                WHERE mm.merge_id = mm.server_role_ban_id;

                -- Insert role ban player records.
                INSERT INTO
                	ban_player (user_id, ban_id)
                SELECT
                	player_user_id, im.ban_id
                FROM
                	_role_ban_import_id_map im
                INNER JOIN _role_ban_import_merge_map mm
                ON im.merge_id = mm.merge_id
                INNER JOIN server_role_ban srb
                ON srb.server_role_ban_id = im.merge_id
                WHERE mm.merge_id = mm.server_role_ban_id
                	AND player_user_id IS NOT NULL;

                -- Insert role ban address records.
                INSERT INTO
                	ban_address (address, ban_id)
                SELECT
                	address, im.ban_id
                FROM
                	_role_ban_import_id_map im
                INNER JOIN _role_ban_import_merge_map mm
                ON im.merge_id = mm.merge_id
                INNER JOIN server_role_ban srb
                ON srb.server_role_ban_id = im.merge_id
                WHERE mm.merge_id = mm.server_role_ban_id
                	AND address IS NOT NULL;

                -- Insert role ban HWID records.
                INSERT INTO
                	ban_hwid (hwid, hwid_type, ban_id)
                SELECT
                	hwid, hwid_type, im.ban_id
                FROM
                	_role_ban_import_id_map im
                INNER JOIN _role_ban_import_merge_map mm
                ON im.merge_id = mm.merge_id
                INNER JOIN server_role_ban srb
                ON srb.server_role_ban_id = im.merge_id
                WHERE mm.merge_id = mm.server_role_ban_id
                	AND hwid IS NOT NULL;

                -- Insert role ban role records.
                INSERT INTO
                	ban_role (role_type, role_id, ban_id)
                SELECT
                	substr(role_id, 1, instr(role_id, ':')-1),
                	substr(role_id, instr(role_id, ':')+1),
                	im.ban_id
                FROM
                	_role_ban_import_id_map im
                INNER JOIN _role_ban_import_merge_map mm
                ON im.merge_id = mm.merge_id
                INNER JOIN server_role_ban srb
                ON srb.server_role_ban_id = mm.server_role_ban_id
                -- Yes, we have some messy ban records which, after merging, end up with duplicate roles.
                ON CONFLICT DO NOTHING;

                -- Insert role unban records.
                INSERT INTO
                	unban (ban_id, unbanning_admin, unban_time)
                SELECT
                	im.ban_id, unbanning_admin, unban_time
                FROM server_role_unban sru
                INNER JOIN _role_ban_import_id_map im
                ON im.merge_id = sru.ban_id;

                -- Insert role rounds
                INSERT INTO
                	ban_round (round_id, ban_id)
                SELECT
                	round_id, im.ban_id
                FROM
                	_role_ban_import_id_map im
                INNER JOIN _role_ban_import_merge_map mm
                ON im.merge_id = mm.merge_id
                INNER JOIN server_role_ban srb
                ON srb.server_role_ban_id = im.merge_id
                WHERE mm.merge_id = mm.server_role_ban_id
                	AND round_id IS NOT NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_server_ban_hit_ban_ban_id",
                table: "server_ban_hit",
                column: "ban_id",
                principalTable: "ban",
                principalColumn: "ban_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(
                name: "FK_server_ban_hit_server_ban_ban_id",
                table: "server_ban_hit");

            migrationBuilder.DropTable(
                name: "server_role_unban");

            migrationBuilder.DropTable(
                name: "server_unban");

            migrationBuilder.DropTable(
                name: "server_role_ban");

            migrationBuilder.DropTable(
                name: "server_ban");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration cannot be rolled back");
        }
    }
}
