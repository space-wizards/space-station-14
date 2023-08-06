#!/usr/bin/env python3

# Script for erasing all data about a user from the database.
# Intended for GDPR erasure requests.
#
# NOTE: We recommend implementing a "GDPR Erasure Ban" on the user's last IP/HWID before erasing their data, to prevent abuse.
# This is acceptable under the GDPR as a "legitimate interest" to prevent GDPR erasure being used to avoid moderation/bans.
# You would need to do this *before* running this script, to avoid losing the IP/HWID of the user entirely.

import argparse
import os
import psycopg2
from uuid import UUID

LATEST_DB_MIGRATION = "20230725193102_AdminNotesImprovementsForeignKeys"

def main():
    parser = argparse.ArgumentParser()
    # Yes we need both to reliably pseudonymize the admin_log table.
    parser.add_argument("user_id", help="User ID to erase data for")
    parser.add_argument("user_name", help="User name to erase data for")
    parser.add_argument("--ignore-schema-mismatch", action="store_true")
    parser.add_argument("--connection-string", required=True, help="Database connection string to use. See https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING")

    args = parser.parse_args()

    conn = psycopg2.connect(args.connection_string)
    cur = conn.cursor()

    check_schema_version(cur, args.ignore_schema_mismatch)
    user_id = args.user_id
    user_name = args.user_name

    clear_admin(cur, user_id)
    pseudonymize_admin_log(cur, user_name, user_id)
    clear_assigned_user_id(cur, user_id)
    clear_connection_log(cur, user_id)
    clear_play_time(cur, user_id)
    clear_player(cur, user_id)
    clear_preference(cur, user_id)
    clear_server_ban(cur, user_id)
    clear_server_ban_exemption(cur, user_id)
    clear_server_role_ban(cur, user_id)
    clear_uploaded_resource_log(cur, user_id)
    clear_whitelist(cur, user_id)

    print("Committing...")
    conn.commit()


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


def clear_admin(cur: "psycopg2.cursor", user_id: str):
    print("Clearing admin...")

    cur.execute("""
DELETE FROM
    admin
WHERE
    user_id = %s
""", (user_id,))


def pseudonymize_admin_log(cur: "psycopg2.cursor", user_name: str, user_id: str):
    print("Pseudonymizing admin_log...")

    cur.execute("""
UPDATE
    admin_log l
SET
    message = replace(message, %s, %s)
FROM
    admin_log_player lp
WHERE
    lp.round_id = l.round_id AND lp.log_id = l.admin_log_id AND player_user_id = %s;
""", (user_name, user_id, user_id,))


def clear_assigned_user_id(cur: "psycopg2.cursor", user_id: str):
    print("Clearing assigned_user_id...")

    cur.execute("""
DELETE FROM
    assigned_user_id
WHERE
    user_id = %s
""", (user_id,))


def clear_connection_log(cur: "psycopg2.cursor", user_id: str):
    print("Clearing connection_log...")

    cur.execute("""
DELETE FROM
    connection_log
WHERE
    user_id = %s
""", (user_id,))


def clear_play_time(cur: "psycopg2.cursor", user_id: str):
    print("Clearing play_time...")

    cur.execute("""
DELETE FROM
    play_time
WHERE
    player_id = %s
""", (user_id,))


def clear_player(cur: "psycopg2.cursor", user_id: str):
    print("Clearing player...")

    cur.execute("""
DELETE FROM
    player
WHERE
    user_id = %s
""", (user_id,))


def clear_preference(cur: "psycopg2.cursor", user_id: str):
    print("Clearing preference...")

    cur.execute("""
DELETE FROM
    preference
WHERE
    user_id = %s
""", (user_id,))


def clear_server_ban(cur: "psycopg2.cursor", user_id: str):
    print("Clearing server_ban...")

    cur.execute("""
DELETE FROM
    server_ban
WHERE
    player_user_id = %s
""", (user_id,))


def clear_server_ban_exemption(cur: "psycopg2.cursor", user_id: str):
    print("Clearing server_ban_exemption...")

    cur.execute("""
DELETE FROM
    server_ban_exemption
WHERE
    user_id = %s
""", (user_id,))


def clear_server_role_ban(cur: "psycopg2.cursor", user_id: str):
    print("Clearing server_role_ban...")

    cur.execute("""
DELETE FROM
    server_role_ban
WHERE
    player_user_id = %s
""", (user_id,))


def clear_uploaded_resource_log(cur: "psycopg2.cursor", user_id: str):
    print("Clearing uploaded_resource_log...")

    cur.execute("""
DELETE FROM
    uploaded_resource_log
WHERE
    user_id = %s
""", (user_id,))


def clear_whitelist(cur: "psycopg2.cursor", user_id: str):
    print("Clearing whitelist...")

    cur.execute("""
DELETE FROM
    whitelist
WHERE
    user_id = %s
""", (user_id,))


main()

# "I'm surprised you managed to write this entire Python file without spamming the word 'sus' everywhere." - Remie

