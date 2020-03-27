#Python 3+ Script for importing admins.txt and admin_ranks.txt made by Jordie0608
#
#Before starting ensure you have installed the mysqlclient package https://github.com/PyMySQL/mysqlclient-python
#It can be downloaded from command line with pip:
#pip install mysqlclient
#And that you have run the most recent commands listed in database_changelog.txt
#
#To view the parameters for this script, execute it with the argument --help
#All the positional arguments are required, remember to include prefixes in your table names if you use them
#An example of the command used to execute this script from powershell:
#python admin_import_2018-02-03.py "localhost" "root" "password" "feedback" "SS13_admin" "SS13_admin_ranks"
#
#This script performs no error-correction, improper configurations of admins.txt or admin_ranks.txt will cause either breaking exceptions or invalid table rows
#It's safe to run this script with your game server(s) active.


import MySQLdb
import argparse
import re
import sys
import string

def parse_text_flags(text, previous):
    flag_values = {"BUILDMODE":1, "BUILD":1, "ADMIN":2, "REJUVINATE":2, "REJUV":2, "BAN":4, "FUN":8, "SERVER":16, "DEBUG":32, "POSSESS":64, "PERMISSIONS":128, "RIGHTS":128, "STEALTH":256, "POLL":512, "VAREDIT":1024, "SOUNDS":2048, "SOUND":2048, "SPAWN":4096, "CREATE":4096, "AUTOLOGIN":8192, "AUTOADMIN":8192, "DBRANKS":16384}
    flags_int = 8192
    exclude_flags_int = 0
    can_edit_flags_int = 0
    flags = text.split(" ")
    if flags:
        for flag in flags:
            sign = flag[:1]
            if flag[1:] in ("@", "prev"):
                if sign is "+":
                    flags_int = previous[0]
                elif sign is "-":
                    exclude_flags_int = previous[1]
                elif sign is "*":
                    can_edit_flags_int = previous[2]
                continue
            if flag[1:] in ("EVERYTHING", "HOST", "ALL"):
                if sign is "+":
                    flags_int = 65535
                elif sign is "-":
                    exclude_flags_int = 65535
                elif sign is "*":
                    can_edit_flags_int = 65535
                continue
            if flag[1:] in flag_values:
                if sign is "+":
                    flags_int += flag_values[flag[1:]]
                elif sign is "-":
                    exclude_flags_int += flag_values[flag[1:]]
                elif sign is "*":
                    can_edit_flags_int += flag_values[flag[1:]]
    flags_int = max(min(65535, flags_int), 0)
    exclude_flags_int = max(min(65535, exclude_flags_int), 0)
    can_edit_flags_int = max(min(65535, can_edit_flags_int), 0)
    return flags_int, exclude_flags_int, can_edit_flags_int

if sys.version_info[0] < 3:
    raise Exception("Python must be at least version 3 for this script.")
parser = argparse.ArgumentParser()
parser.add_argument("address", help="MySQL server address (use localhost for the current computer)")
parser.add_argument("username", help="MySQL login username")
parser.add_argument("password", help="MySQL login username")
parser.add_argument("database", help="Database name")
parser.add_argument("admintable", help="Name of the current admin table (remember prefixes if you use them)")
parser.add_argument("rankstable", help="Name of the current admin ranks (remember prefixes)")
args = parser.parse_args()
db=MySQLdb.connect(host=args.address, user=args.username, passwd=args.password, db=args.database)
cursor=db.cursor()
ranks_table = args.rankstable
admin_table = args.admintable
ckeyExformat = re.sub("@|-|_", " ", string.punctuation)
with open("..\\config\\admin_ranks.txt") as rank_file:
    previous = 0
    for line in rank_file:
        if line.strip():
            if line.startswith("#"):
                continue
            matches = re.match("(.+)\\b\\s+=\\s*(.*)", line)
            rank = "".join((c for c in matches.group(1) if c not in ckeyExformat))
            flags = parse_text_flags(matches.group(2), previous)
            previous = flags
            cursor.execute("INSERT INTO {0} (rank, flags, exclude_flags, can_edit_flags) VALUES ('{1}', {2}, {3}, {4})".format(ranks_table, rank, flags[0], flags[1], flags[2]))
with open("..\\config\\admins.txt") as admins_file:
    previous = 0
    ckeyformat = string.punctuation.replace("@", " ")
    for line in admins_file:
        if line.strip():
            if line.startswith("#"):
                continue
            matches = re.match("(.+)\\b\\s+=\\s+(.+)", line)
            ckey = "".join((c for c in matches.group(1) if c not in ckeyformat)).lower()
            rank = "".join((c for c in matches.group(2) if c not in ckeyExformat))
            cursor.execute("INSERT INTO {0} (ckey, rank) VALUES ('{1}', '{2}')".format(admin_table, ckey, rank))
db.commit()
cursor.close()
print("Import complete.")
