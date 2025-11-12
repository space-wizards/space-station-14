#!/usr/bin/env python3

# User data dumping script for dumping data from an SS14 postgres database.
# Intended to service GDPR data requests or what have you.

import argparse
import os
import psycopg2
from uuid import UUID

LATEST_DB_MIGRATION = "20250314222016_ConstructionFavorites"

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("output", help="Directory to output data dumps into.")
    parser.add_argument("user", help="User name/ID to dump data into.")
    parser.add_argument("--ignore-schema-mismatch", action="store_true")
    parser.add_argument("--connection-string", required=True, help="Database connection string to use. See https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING")

    args = parser.parse_args()

    arg_output: str = args.output

    if not os.path.exists(arg_output):
        print("Creating output directory (doesn't exist yet)")
        os.mkdir(arg_output)

    conn = psycopg2.connect(args.connection_string)
    cur = conn.cursor()

    check_schema_version(cur, args.ignore_schema_mismatch)
    user_id = normalize_user_id(cur, args.user)

    dump_admin(cur, user_id, arg_output)
    dump_admin_log(cur, user_id, arg_output)
    dump_admin_messages(cur, user_id, arg_output)
    dump_admin_notes(cur, user_id, arg_output)
    dump_admin_watchlists(cur, user_id, arg_output)
    dump_blacklist(cur, user_id, arg_output)
    dump_connection_log(cur, user_id, arg_output)
    dump_play_time(cur, user_id, arg_output)
    dump_player(cur, user_id, arg_output)
    dump_preference(cur, user_id, arg_output)
    dump_role_whitelists(cur, user_id, arg_output)
    dump_server_ban(cur, user_id, arg_output)
    dump_server_ban_exemption(cur, user_id, arg_output)
    dump_server_role_ban(cur, user_id, arg_output)
    dump_uploaded_resource_log(cur, user_id, arg_output)
    dump_whitelist(cur, user_id, arg_output)


def check_schema_version(cur: "psycopg2.cursor", ignore_mismatch: bool):
    cur.execute('SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "__EFMigrationsHistory" DESC LIMIT 1')
    schema_version = cur.fetchone()
    if schema_version == None:
        print("Unable to read database schema version.")
        exit(1)

    if schema_version[0] != LATEST_DB_MIGRATION:
        print(f"Unsupport schema version of DB: '{schema_version[0]}'. Supported: {LATEST_DB_MIGRATION}")
        if ignore_mismatch:
            return
        exit(1)


def normalize_user_id(cur: "psycopg2.cursor", name_or_uid: str) -> str:
    try:
        return str(UUID(name_or_uid))
    except ValueError:
        # Must be a name, get UUID from DB.
        pass

    cur.execute("SELECT user_id FROM player WHERE last_seen_user_name = %s ORDER BY last_seen_time DESC LIMIT 1", (name_or_uid,))
    row = cur.fetchone()
    if row == None:
        print(f"Unable to find user '{name_or_uid}' in DB.")
        exit(1)

    print(f"Found user ID: {row[0]}")
    return row[0]


def dump_admin(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping admin...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data) - 'admin_rank_id'), '[]') #>> '{}'
FROM (
    SELECT
        *,
        (SELECT to_json(rank) FROM (
            SELECT * FROM admin_rank WHERE admin_rank.admin_rank_id = admin.admin_rank_id
        ) rank)
        as admin_rank,
        (SELECT COALESCE(json_agg(to_jsonb(flagg) - 'admin_id'), '[]') FROM (
            SELECT * FROM admin_flag WHERE admin_id = %s
        ) flagg)
        as admin_flags
    FROM
        admin
    WHERE
        user_id = %s
) as data
""", (user_id, user_id))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "admin.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_admin_log(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping admin_log...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data) - 'admin_log_id'), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        admin_log_player alp
    INNER JOIN
        admin_log al
    ON
        al.admin_log_id = alp.log_id AND al.round_id = alp.round_id
    WHERE
        player_user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "admin_log.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_admin_notes(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping admin_notes...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        admin_notes
    WHERE
        player_user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "admin_notes.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_connection_log(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping connection_log...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *,
        (SELECT COALESCE(json_agg(to_jsonb(ban_hit)), '[]') FROM (
            SELECT * FROM server_ban_hit WHERE connection_id = connection_log_id
        ) ban_hit)
        as ban_hits
    FROM
        connection_log
    WHERE
        user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "connection_log.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_play_time(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping play_time...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        play_time
    WHERE
        player_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "play_time.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_player(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping player...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *,
        (SELECT COALESCE(json_agg(to_jsonb(player_round_subquery) - 'players_id'), '[]') FROM (
            SELECT * FROM player_round WHERE players_id = player_id
        ) player_round_subquery)
        as player_rounds
    FROM
        player
    WHERE
        user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "player.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_preference(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping preference...")

    # God have mercy on my soul.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *,
        (SELECT json_agg(to_jsonb(profile_subq) - 'preference_id') FROM (
            SELECT
                *,
                (SELECT COALESCE(json_agg(to_jsonb(job_subq) - 'profile_id'), '[]') FROM (
                    SELECT * FROM job WHERE job.profile_id = profile.profile_id
                ) job_subq)
                as jobs,
                (SELECT COALESCE(json_agg(to_jsonb(antag_subq) - 'profile_id'), '[]') FROM (
                    SELECT * FROM antag WHERE antag.profile_id = profile.profile_id
                ) antag_subq)
                as antags,
                (SELECT COALESCE(json_agg(to_jsonb(trait_subq) - 'profile_id'), '[]') FROM (
                    SELECT * FROM trait WHERE trait.profile_id = profile.profile_id
                ) trait_subq)
                as traits,
                (SELECT COALESCE(json_agg(to_jsonb(role_loadout_subq) - 'profile_id'), '[]') FROM (
                    SELECT
                        *,
                        (SELECT COALESCE(json_agg(to_jsonb(loadout_group_subq) - 'profile_role_loadout_id'), '[]') FROM (
                            SELECT
                                *,
                                (SELECT COALESCE(json_agg(to_jsonb(loadout_subq) - 'profile_loadout_group_id'), '[]') FROM (
                                    SELECT * FROM profile_loadout WHERE profile_loadout.profile_loadout_group_id = profile_loadout_group.profile_loadout_group_id
                                ) loadout_subq)
                                as loadouts
                            FROM
                                profile_loadout_group
                            WHERE
                                profile_loadout_group.profile_role_loadout_id = profile_role_loadout.profile_role_loadout_id
                        ) loadout_group_subq)
                        as loadout_groups
                    FROM
                        profile_role_loadout
                    WHERE
                        profile_role_loadout.profile_id = profile.profile_id
                ) role_loadout_subq)
                as role_loadouts
            FROM
                profile
            WHERE
                profile.preference_id = preference.preference_id
        ) profile_subq)
        as profiles
    FROM
        preference
    WHERE
        user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "preference.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_server_ban(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping server_ban...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *,
        (SELECT to_jsonb(unban_sq) - 'ban_id' FROM (
            SELECT * FROM server_unban WHERE server_unban.ban_id = server_ban.server_ban_id
        ) unban_sq)
        as unban
    FROM
        server_ban
    WHERE
        player_user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "server_ban.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_server_ban_exemption(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping server_ban_exemption...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        server_ban_exemption
    WHERE
        user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "server_ban_exemption.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_server_role_ban(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping server_role_ban...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *,
        (SELECT to_jsonb(role_unban_sq) - 'ban_id' FROM (
            SELECT * FROM server_role_unban WHERE server_role_unban.ban_id = server_role_ban.server_role_ban_id
        ) role_unban_sq)
        as unban
    FROM
        server_role_ban
    WHERE
        player_user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "server_role_ban.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_uploaded_resource_log(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping uploaded_resource_log...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        uploaded_resource_log
    WHERE
        user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "uploaded_resource_log.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_whitelist(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping whitelist...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        whitelist
    WHERE
        user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "whitelist.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_blacklist(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping blacklist...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        blacklist
    WHERE
        user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "blacklist.json"), "w", encoding="utf-8") as f:
        f.write(json_data)

def dump_role_whitelists(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping role_whitelists...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        role_whitelists
    WHERE
        player_user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "role_whitelists.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_admin_messages(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping admin_messages...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        admin_messages
    WHERE
        player_user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "admin_messages.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_admin_watchlists(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping admin_watchlists...")

    cur.execute("""
SELECT
    COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        admin_watchlists
    WHERE
        player_user_id = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "admin_watchlists.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


main()

# "I'm surprised you managed to write this entire Python file without spamming the word 'sus' everywhere." - Remie

