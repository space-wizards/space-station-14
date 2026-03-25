using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdminLogsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_rank",
                columns: table => new
                {
                    admin_rank_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_rank", x => x.admin_rank_id);
                });

            migrationBuilder.CreateTable(
                name: "assigned_user_id",
                columns: table => new
                {
                    assigned_user_id_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_name = table.Column<string>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_user_id", x => x.assigned_user_id_id);
                });

            migrationBuilder.CreateTable(
                name: "ban_template",
                columns: table => new
                {
                    ban_template_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    length = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    exempt_flags = table.Column<int>(type: "INTEGER", nullable: false),
                    severity = table.Column<int>(type: "INTEGER", nullable: false),
                    auto_delete = table.Column<bool>(type: "INTEGER", nullable: false),
                    hidden = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ban_template", x => x.ban_template_id);
                });

            migrationBuilder.CreateTable(
                name: "blacklist",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blacklist", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "ipintel_cache",
                columns: table => new
                {
                    ipintel_cache_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    address = table.Column<string>(type: "TEXT", nullable: false),
                    time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    score = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ipintel_cache", x => x.ipintel_cache_id);
                });

            migrationBuilder.CreateTable(
                name: "play_time",
                columns: table => new
                {
                    play_time_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tracker = table.Column<string>(type: "TEXT", nullable: false),
                    time_spent = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_play_time", x => x.play_time_id);
                });

            migrationBuilder.CreateTable(
                name: "player",
                columns: table => new
                {
                    player_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    first_seen_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_seen_user_name = table.Column<string>(type: "TEXT", nullable: false),
                    last_seen_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_seen_address = table.Column<string>(type: "TEXT", nullable: false),
                    last_seen_hwid = table.Column<byte[]>(type: "BLOB", nullable: true),
                    last_seen_hwid_type = table.Column<int>(type: "INTEGER", nullable: true, defaultValue: 0),
                    last_read_rules = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player", x => x.player_id);
                    table.UniqueConstraint("ak_player_user_id", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "preference",
                columns: table => new
                {
                    preference_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    selected_character_slot = table.Column<int>(type: "INTEGER", nullable: false),
                    admin_ooc_color = table.Column<string>(type: "TEXT", nullable: false),
                    construction_favorites = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preference", x => x.preference_id);
                });

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    server_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server", x => x.server_id);
                });

            migrationBuilder.CreateTable(
                name: "server_ban_exemption",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    flags = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_ban_exemption", x => x.user_id);
                    table.CheckConstraint("FlagsNotZero", "flags != 0");
                });

            migrationBuilder.CreateTable(
                name: "uploaded_resource_log",
                columns: table => new
                {
                    uploaded_resource_log_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    path = table.Column<string>(type: "TEXT", nullable: false),
                    data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uploaded_resource_log", x => x.uploaded_resource_log_id);
                });

            migrationBuilder.CreateTable(
                name: "whitelist",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whitelist", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "admin",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: true),
                    deadminned = table.Column<bool>(type: "INTEGER", nullable: false),
                    suspended = table.Column<bool>(type: "INTEGER", nullable: false),
                    admin_rank_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_admin_admin_rank_admin_rank_id",
                        column: x => x.admin_rank_id,
                        principalTable: "admin_rank",
                        principalColumn: "admin_rank_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "admin_rank_flag",
                columns: table => new
                {
                    admin_rank_flag_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    flag = table.Column<string>(type: "TEXT", nullable: false),
                    admin_rank_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_rank_flag", x => x.admin_rank_flag_id);
                    table.ForeignKey(
                        name: "FK_admin_rank_flag_admin_rank_admin_rank_id",
                        column: x => x.admin_rank_id,
                        principalTable: "admin_rank",
                        principalColumn: "admin_rank_id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "role_whitelists",
                columns: table => new
                {
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    role_id = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_whitelists", x => new { x.player_user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_role_whitelists_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    slot = table.Column<int>(type: "INTEGER", nullable: false),
                    char_name = table.Column<string>(type: "TEXT", nullable: false),
                    flavor_text = table.Column<string>(type: "TEXT", nullable: false),
                    age = table.Column<int>(type: "INTEGER", nullable: false),
                    sex = table.Column<string>(type: "TEXT", nullable: false),
                    gender = table.Column<string>(type: "TEXT", nullable: false),
                    species = table.Column<string>(type: "TEXT", nullable: false),
                    organ_markings = table.Column<byte[]>(type: "jsonb", nullable: true),
                    markings = table.Column<byte[]>(type: "jsonb", nullable: true),
                    hair_name = table.Column<string>(type: "TEXT", nullable: false),
                    hair_color = table.Column<string>(type: "TEXT", nullable: false),
                    facial_hair_name = table.Column<string>(type: "TEXT", nullable: false),
                    facial_hair_color = table.Column<string>(type: "TEXT", nullable: false),
                    eye_color = table.Column<string>(type: "TEXT", nullable: false),
                    skin_color = table.Column<string>(type: "TEXT", nullable: false),
                    spawn_priority = table.Column<int>(type: "INTEGER", nullable: false),
                    pref_unavailable = table.Column<int>(type: "INTEGER", nullable: false),
                    preference_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_profile_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_entity_dimension",
                columns: table => new
                {
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    entity_uid = table.Column<int>(type: "INTEGER", nullable: false),
                    prototype_id = table.Column<string>(type: "TEXT", nullable: true),
                    entity_name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_entity_dimension", x => new { x.server_id, x.round_id, x.entity_uid });
                    table.ForeignKey(
                        name: "FK_admin_log_entity_dimension_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connection_log",
                columns: table => new
                {
                    connection_log_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_name = table.Column<string>(type: "TEXT", nullable: false),
                    time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    address = table.Column<string>(type: "TEXT", nullable: false),
                    hwid = table.Column<byte[]>(type: "BLOB", nullable: true),
                    hwid_type = table.Column<int>(type: "INTEGER", nullable: true, defaultValue: 0),
                    denied = table.Column<byte>(type: "INTEGER", nullable: true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    trust = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connection_log", x => x.connection_log_id);
                    table.ForeignKey(
                        name: "FK_connection_log_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "round",
                columns: table => new
                {
                    round_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    start_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_round", x => x.round_id);
                    table.ForeignKey(
                        name: "FK_round_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_flag",
                columns: table => new
                {
                    admin_flag_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    flag = table.Column<string>(type: "TEXT", nullable: false),
                    negative = table.Column<bool>(type: "INTEGER", nullable: false),
                    admin_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_flag", x => x.admin_flag_id);
                    table.ForeignKey(
                        name: "FK_admin_flag_admin_admin_id",
                        column: x => x.admin_id,
                        principalTable: "admin",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "antag",
                columns: table => new
                {
                    antag_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    antag_name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antag", x => x.antag_id);
                    table.ForeignKey(
                        name: "FK_antag_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job",
                columns: table => new
                {
                    job_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    job_name = table.Column<string>(type: "TEXT", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job", x => x.job_id);
                    table.ForeignKey(
                        name: "FK_job_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_role_loadout",
                columns: table => new
                {
                    profile_role_loadout_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    role_name = table.Column<string>(type: "TEXT", nullable: false),
                    entity_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_role_loadout", x => x.profile_role_loadout_id);
                    table.ForeignKey(
                        name: "FK_profile_role_loadout_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trait",
                columns: table => new
                {
                    trait_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    trait_name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trait", x => x.trait_id);
                    table.ForeignKey(
                        name: "FK_trait_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "server_ban_hit",
                columns: table => new
                {
                    server_ban_hit_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ban_id = table.Column<int>(type: "INTEGER", nullable: false),
                    connection_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_ban_hit", x => x.server_ban_hit_id);
                    table.ForeignKey(
                        name: "FK_server_ban_hit_ban_ban_id",
                        column: x => x.ban_id,
                        principalTable: "ban",
                        principalColumn: "ban_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_server_ban_hit_connection_log_connection_id",
                        column: x => x.connection_id,
                        principalTable: "connection_log",
                        principalColumn: "connection_log_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_event",
                columns: table => new
                {
                    admin_log_event_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false),
                    impact = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_event", x => x.admin_log_event_id);
                    table.ForeignKey(
                        name: "FK_admin_log_event_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_log_event_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_messages",
                columns: table => new
                {
                    admin_messages_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    created_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    seen = table.Column<bool>(type: "INTEGER", nullable: false),
                    dismissed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_messages", x => x.admin_messages_id);
                    table.CheckConstraint("NotDismissedAndSeen", "NOT dismissed OR seen");
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_messages_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateTable(
                name: "admin_notes",
                columns: table => new
                {
                    admin_notes_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    severity = table.Column<int>(type: "INTEGER", nullable: false),
                    created_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    secret = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_notes", x => x.admin_notes_id);
                    table.ForeignKey(
                        name: "FK_admin_notes_player_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_notes_player_deleted_by_id",
                        column: x => x.deleted_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_notes_player_last_edited_by_id",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_admin_notes_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_notes_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateTable(
                name: "admin_watchlists",
                columns: table => new
                {
                    admin_watchlists_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    playtime_at_note = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    created_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_edited_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expiration_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deleted_by_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_watchlists_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
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
                name: "player_round",
                columns: table => new
                {
                    players_id = table.Column<int>(type: "INTEGER", nullable: false),
                    rounds_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_round", x => new { x.players_id, x.rounds_id });
                    table.ForeignKey(
                        name: "FK_player_round_player_players_id",
                        column: x => x.players_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_player_round_round_rounds_id",
                        column: x => x.rounds_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_loadout_group",
                columns: table => new
                {
                    profile_loadout_group_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_role_loadout_id = table.Column<int>(type: "INTEGER", nullable: false),
                    group_name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_loadout_group", x => x.profile_loadout_group_id);
                    table.ForeignKey(
                        name: "FK_profile_loadout_group_profile_role_loadout_profile_role_loadout_id",
                        column: x => x.profile_role_loadout_id,
                        principalTable: "profile_role_loadout",
                        principalColumn: "profile_role_loadout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_event_participant",
                columns: table => new
                {
                    admin_log_event_participant_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    event_id = table.Column<int>(type: "INTEGER", nullable: false),
                    server_id = table.Column<int>(type: "INTEGER", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false),
                    impact = table.Column<sbyte>(type: "INTEGER", nullable: false),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    entity_uid = table.Column<int>(type: "INTEGER", nullable: true),
                    role = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_event_participant", x => x.admin_log_event_participant_id);
                    table.ForeignKey(
                        name: "FK_admin_log_event_participant_admin_log_event_event_id",
                        column: x => x.event_id,
                        principalTable: "admin_log_event",
                        principalColumn: "admin_log_event_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_log_event_participant_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_log_event_payload",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "INTEGER", nullable: false),
                    message = table.Column<string>(type: "TEXT", nullable: false),
                    json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_log_event_payload", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_admin_log_event_payload_admin_log_event_event_id",
                        column: x => x.event_id,
                        principalTable: "admin_log_event",
                        principalColumn: "admin_log_event_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_loadout",
                columns: table => new
                {
                    profile_loadout_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_loadout_group_id = table.Column<int>(type: "INTEGER", nullable: false),
                    loadout_name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_loadout", x => x.profile_loadout_id);
                    table.ForeignKey(
                        name: "FK_profile_loadout_profile_loadout_group_profile_loadout_group_id",
                        column: x => x.profile_loadout_group_id,
                        principalTable: "profile_loadout_group",
                        principalColumn: "profile_loadout_group_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_admin_rank_id",
                table: "admin",
                column: "admin_rank_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_flag_admin_id",
                table: "admin_flag",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_flag_flag_admin_id",
                table: "admin_flag",
                columns: new[] { "flag", "admin_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_entity_dimension_server_id_entity_uid",
                table: "admin_log_entity_dimension",
                columns: new[] { "server_id", "entity_uid" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_round_id_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "round_id", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_round_id_type_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "round_id", "type", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_impact_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "impact", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_round_id_impact_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "round_id", "impact", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_round_id_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "round_id", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_server_id_type_occurred_at_admin_log_event_id",
                table: "admin_log_event",
                columns: new[] { "server_id", "type", "occurred_at", "admin_log_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_event_id",
                table: "admin_log_event_participant",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id_entity_uid_occurred_at_event_id",
                table: "admin_log_event_participant",
                columns: new[] { "server_id", "entity_uid", "occurred_at", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id_entity_uid_role_occurred_at_event_id",
                table: "admin_log_event_participant",
                columns: new[] { "server_id", "entity_uid", "role", "occurred_at", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id_player_user_id_occurred_at_event_id",
                table: "admin_log_event_participant",
                columns: new[] { "server_id", "player_user_id", "occurred_at", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_log_event_participant_server_id_round_id_player_user_id_occurred_at_event_id",
                table: "admin_log_event_participant",
                columns: new[] { "server_id", "round_id", "player_user_id", "occurred_at", "event_id" });

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
                name: "IX_admin_notes_created_by_id",
                table: "admin_notes",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_deleted_by_id",
                table: "admin_notes",
                column: "deleted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_last_edited_by_id",
                table: "admin_notes",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_player_user_id",
                table: "admin_notes",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_notes_round_id",
                table: "admin_notes",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_rank_flag_admin_rank_id",
                table: "admin_rank_flag",
                column: "admin_rank_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_rank_flag_flag_admin_rank_id",
                table: "admin_rank_flag",
                columns: new[] { "flag", "admin_rank_id" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_antag_profile_id_antag_name",
                table: "antag",
                columns: new[] { "profile_id", "antag_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assigned_user_id_user_id",
                table: "assigned_user_id",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assigned_user_id_user_name",
                table: "assigned_user_id",
                column: "user_name",
                unique: true);

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
                name: "IX_connection_log_server_id",
                table: "connection_log",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_connection_log_time",
                table: "connection_log",
                column: "time");

            migrationBuilder.CreateIndex(
                name: "IX_connection_log_user_id",
                table: "connection_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ipintel_cache_address",
                table: "ipintel_cache",
                column: "address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_one_high_priority",
                table: "job",
                column: "profile_id",
                unique: true,
                filter: "priority = 3");

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_id",
                table: "job",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_profile_id_job_name",
                table: "job",
                columns: new[] { "profile_id", "job_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_play_time_player_id_tracker",
                table: "play_time",
                columns: new[] { "player_id", "tracker" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_last_seen_user_name",
                table: "player",
                column: "last_seen_user_name");

            migrationBuilder.CreateIndex(
                name: "IX_player_user_id",
                table: "player",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_round_rounds_id",
                table: "player_round",
                column: "rounds_id");

            migrationBuilder.CreateIndex(
                name: "IX_preference_user_id",
                table: "preference",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_preference_id",
                table: "profile",
                column: "preference_id");

            migrationBuilder.CreateIndex(
                name: "IX_profile_slot_preference_id",
                table: "profile",
                columns: new[] { "slot", "preference_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_loadout_profile_loadout_group_id",
                table: "profile_loadout",
                column: "profile_loadout_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_profile_loadout_group_profile_role_loadout_id",
                table: "profile_loadout_group",
                column: "profile_role_loadout_id");

            migrationBuilder.CreateIndex(
                name: "IX_profile_role_loadout_profile_id",
                table: "profile_role_loadout",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_round_server_id",
                table: "round",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "IX_round_start_date",
                table: "round",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_hit_ban_id",
                table: "server_ban_hit",
                column: "ban_id");

            migrationBuilder.CreateIndex(
                name: "IX_server_ban_hit_connection_id",
                table: "server_ban_hit",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "IX_trait_profile_id_trait_name",
                table: "trait",
                columns: new[] { "profile_id", "trait_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unban_ban_id",
                table: "unban",
                column: "ban_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_flag");

            migrationBuilder.DropTable(
                name: "admin_log_entity_dimension");

            migrationBuilder.DropTable(
                name: "admin_log_event_participant");

            migrationBuilder.DropTable(
                name: "admin_log_event_payload");

            migrationBuilder.DropTable(
                name: "admin_messages");

            migrationBuilder.DropTable(
                name: "admin_notes");

            migrationBuilder.DropTable(
                name: "admin_rank_flag");

            migrationBuilder.DropTable(
                name: "admin_watchlists");

            migrationBuilder.DropTable(
                name: "antag");

            migrationBuilder.DropTable(
                name: "assigned_user_id");

            migrationBuilder.DropTable(
                name: "ban_address");

            migrationBuilder.DropTable(
                name: "ban_hwid");

            migrationBuilder.DropTable(
                name: "ban_player");

            migrationBuilder.DropTable(
                name: "ban_role");

            migrationBuilder.DropTable(
                name: "ban_round");

            migrationBuilder.DropTable(
                name: "ban_template");

            migrationBuilder.DropTable(
                name: "blacklist");

            migrationBuilder.DropTable(
                name: "ipintel_cache");

            migrationBuilder.DropTable(
                name: "job");

            migrationBuilder.DropTable(
                name: "play_time");

            migrationBuilder.DropTable(
                name: "player_round");

            migrationBuilder.DropTable(
                name: "profile_loadout");

            migrationBuilder.DropTable(
                name: "role_whitelists");

            migrationBuilder.DropTable(
                name: "server_ban_exemption");

            migrationBuilder.DropTable(
                name: "server_ban_hit");

            migrationBuilder.DropTable(
                name: "trait");

            migrationBuilder.DropTable(
                name: "unban");

            migrationBuilder.DropTable(
                name: "uploaded_resource_log");

            migrationBuilder.DropTable(
                name: "whitelist");

            migrationBuilder.DropTable(
                name: "admin");

            migrationBuilder.DropTable(
                name: "admin_log_event");

            migrationBuilder.DropTable(
                name: "profile_loadout_group");

            migrationBuilder.DropTable(
                name: "connection_log");

            migrationBuilder.DropTable(
                name: "ban");

            migrationBuilder.DropTable(
                name: "admin_rank");

            migrationBuilder.DropTable(
                name: "round");

            migrationBuilder.DropTable(
                name: "profile_role_loadout");

            migrationBuilder.DropTable(
                name: "player");

            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "preference");
        }
    }
}
