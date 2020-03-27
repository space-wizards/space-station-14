#Python 3+ Script for converting ban table format as of 2018-10-28 made by Jordie0608
#
#Before starting ensure you have installed the mysqlclient package https://github.com/PyMySQL/mysqlclient-python
#It can be downloaded from command line with pip:
#pip install mysqlclient
#
#You will also have to create a new ban table for inserting converted data to per the schema:
#CREATE TABLE `ban` (
#  `id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
#  `bantime` DATETIME NOT NULL,
#  `server_ip` INT(10) UNSIGNED NOT NULL,
#  `server_port` SMALLINT(5) UNSIGNED NOT NULL,
#  `round_id` INT(11) UNSIGNED NOT NULL,
#  `role` VARCHAR(32) NULL DEFAULT NULL,
#  `expiration_time` DATETIME NULL DEFAULT NULL,
#  `applies_to_admins` TINYINT(1) UNSIGNED NOT NULL DEFAULT '0',
#  `reason` VARCHAR(2048) NOT NULL,
#  `ckey` VARCHAR(32) NULL DEFAULT NULL,
#  `ip` INT(10) UNSIGNED NULL DEFAULT NULL,
#  `computerid` VARCHAR(32) NULL DEFAULT NULL,
#  `a_ckey` VARCHAR(32) NOT NULL,
#  `a_ip` INT(10) UNSIGNED NOT NULL,
#  `a_computerid` VARCHAR(32) NOT NULL,
#  `who` VARCHAR(2048) NOT NULL,
#  `adminwho` VARCHAR(2048) NOT NULL,
#  `edits` TEXT NULL DEFAULT NULL,
#  `unbanned_datetime` DATETIME NULL DEFAULT NULL,
#  `unbanned_ckey` VARCHAR(32) NULL DEFAULT NULL,
#  `unbanned_ip` INT(10) UNSIGNED NULL DEFAULT NULL,
#  `unbanned_computerid` VARCHAR(32) NULL DEFAULT NULL,
#  `unbanned_round_id` INT(11) UNSIGNED NULL DEFAULT NULL,
#  PRIMARY KEY (`id`),
#  KEY `idx_ban_isbanned` (`ckey`,`role`,`unbanned_datetime`,`expiration_time`),
#  KEY `idx_ban_isbanned_details` (`ckey`,`ip`,`computerid`,`role`,`unbanned_datetime`,`expiration_time`),
#  KEY `idx_ban_count` (`bantime`,`a_ckey`,`applies_to_admins`,`unbanned_datetime`,`expiration_time`)
#) ENGINE=InnoDB DEFAULT CHARSET=latin1;
#This is to prevent the destruction of existing data and allow rollbacks to be performed in the event of an error during conversion
#Once conversion is complete remember to rename the old and new ban tables; it's up to you if you want to keep the old table
#
#To view the parameters for this script, execute it with the argument --help
#All the positional arguments are required, remember to include prefixes in your table names if you use them
#An example of the command used to execute this script from powershell:
#python ban_conversion_2018-10-28.py "localhost" "root" "password" "feedback" "SS13_ban" "SS13_ban_new"
#I found that this script would complete conversion of 35000 rows in approximately 20 seconds, results will depend on the size of your ban table and computer used
#
#The script has been tested to complete with tgstation's ban table as of 2018-09-02 02:19:56
#In the event of an error the new ban table is automatically truncated
#The source table is never modified so you don't have to worry about losing any data due to errors
#Some additional error correction is performed to fix problems specific to legacy and invalid data in tgstation's ban table, these operations are tagged with a 'TG:' comment
#Even if you don't have any of these specific problems in your ban table the operations won't have matter as they have an insignificant effect on runtime
#
#While this script is safe to run with your game server(s) active, any bans created after the script has started won't be converted
#You will also have to ensure that the code and table names are updated between rounds as neither will be compatible

import MySQLdb
import argparse
import sys
from datetime import datetime

def parse_role(bantype, job):
    if bantype in ("PERMABAN", "TEMPBAN", "ADMIN_PERMABAN", "ADMIN_TEMPBAN"):
        role = "Server"
    else:
        #TG: Some legacy jobbans are missing the last character from their job string.
        job_name_fixes = {"A":"AI", "Captai":"Captain", "Cargo Technicia":"Cargo Technician", "Chaplai":"Chaplain", "Che":"Chef", "Chemis":"Chemist", "Chief Enginee":"Chief Engineer", "Chief Medical Office":"Chief Medical Officer", "Cybor":"Cyborg", "Detectiv":"Detective", "Head of Personne":"Head of Personnel", "Head of Securit":"Head of Security", "Mim":"Mime", "pA":"pAI", "Quartermaste":"Quartermaster", "Research Directo":"Research Director", "Scientis":"Scientist", "Security Office":"Security Officer", "Station Enginee":"Station Engineer", "Syndicat":"Syndicate", "Warde":"Warden"}
        keep_job_names = ("AI", "Head of Personnel", "Head of Security", "OOC", "pAI")
        if job in job_name_fixes:
            role = job_name_fixes[job]
        #Some job names we want to keep the same as .title() would return a different string.
        elif job in keep_job_names:
            role = job
        #And then there's this asshole.
        elif job == "servant of Ratvar":
            role = "Servant of Ratvar"
        else:
            role = job.title()
    return role

def parse_admin(bantype):
    if bantype in ("ADMIN_PERMABAN", "ADMIN_TEMPBAN"):
        return 1
    else:
        return 0

def parse_datetime(bantype, expiration_time):
    if bantype in ("PERMABAN", "JOB_PERMABAN", "ADMIN_PERMABAN"):
        expiration_time = None
    #TG: two bans with an invalid expiration_time due to admins setting the duration to approx. 19 billion years, I'm going to count them as permabans.
    elif expiration_time == "0000-00-00 00:00:00":
        expiration_time = None
    elif not expiration_time:
        expiration_time = None
    return expiration_time

def parse_not_null(field):
    if not field:
        field = 0
    return field

def parse_for_empty(field):
    if not field:
        field = None
    #TG: Several bans from 2012, probably from clients disconnecting while a ban was being made.
    elif field == "BLANK CKEY ERROR":
        field = None
    return field

if sys.version_info[0] < 3:
    raise Exception("Python must be at least version 3 for this script.")
current_round = 0
parser = argparse.ArgumentParser()
parser.add_argument("address", help="MySQL server address (use localhost for the current computer)")
parser.add_argument("username", help="MySQL login username")
parser.add_argument("password", help="MySQL login username")
parser.add_argument("database", help="Database name")
parser.add_argument("curtable", help="Name of the current ban table (remember prefixes if you use them)")
parser.add_argument("newtable", help="Name of the new table to insert to, can't be same as the source table (remember prefixes)")
args = parser.parse_args()
db=MySQLdb.connect(host=args.address, user=args.username, passwd=args.password, db=args.database)
cursor=db.cursor()
current_table = args.curtable
new_table = args.newtable
#TG: Due to deleted rows and a legacy ban import being inserted from id 3140 id order is not contiguous or in line with date order. While technically valid, it's confusing and I don't like that.
#TG: So instead of just running through to MAX(id) we're going to reorder the records by bantime as we go.
cursor.execute("SELECT id FROM " + current_table + " ORDER BY bantime ASC")
id_list = cursor.fetchall()
start_time = datetime.now()
print("Beginning conversion at {0}".format(start_time.strftime("%Y-%m-%d %H:%M:%S")))
try:
    for current_id in id_list:
        if current_id[0] % 5000 == 0:
            cur_time = datetime.now()
            print("Reached row ID {0} Duration: {1}".format(current_id[0], cur_time - start_time))
        cursor.execute("SELECT * FROM " + current_table + " WHERE id = %s", [current_id[0]])
        query_row = cursor.fetchone()
        if not query_row:
            continue
        else:
            #TG: bans with an empty reason which were somehow created with almost every field being null or empty, we can't do much but skip this
            if not query_row[6]:
                continue
            bantime = query_row[1]
            server_ip = query_row[2]
            server_port = query_row[3]
            round_id = query_row[4]
            applies_to_admins = parse_admin(query_row[5])
            reason = query_row[6]
            role = parse_role(query_row[5], query_row[7])
            expiration_time = parse_datetime(query_row[5], query_row[9])
            ckey = parse_for_empty(query_row[10])
            computerid = parse_for_empty(query_row[11])
            ip = parse_for_empty(query_row[12])
            a_ckey = parse_not_null(query_row[13])
            a_computerid = parse_not_null(query_row[14])
            a_ip = parse_not_null(query_row[15])
            who = query_row[16]
            adminwho = query_row[17]
            edits = parse_for_empty(query_row[18])
            unbanned_datetime = parse_datetime(None, query_row[20])
            unbanned_ckey = parse_for_empty(query_row[21])
            unbanned_computerid = parse_for_empty(query_row[22])
            unbanned_ip = parse_for_empty(query_row[23])
            cursor.execute("INSERT INTO " + new_table + " (bantime, server_ip, server_port, round_id, role, expiration_time, applies_to_admins, reason, ckey, ip, computerid, a_ckey, a_ip, a_computerid, who, adminwho, edits, unbanned_datetime, unbanned_ckey, unbanned_ip, unbanned_computerid) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)", (bantime, server_ip, server_port, round_id, role, expiration_time, applies_to_admins, reason, ckey, ip, computerid, a_ckey, a_ip, a_computerid, who, adminwho, edits, unbanned_datetime, unbanned_ckey, unbanned_ip, unbanned_computerid))
    db.commit()
    end_time = datetime.now()
    print("Conversion completed at {0}".format(datetime.now().strftime("%Y-%m-%d %H:%M:%S")))
    print("Script duration: {0}".format(end_time - start_time))
except Exception as e:
    end_time = datetime.now()
    print("Error encountered on row ID {0} at {1}".format(current_id[0], datetime.now().strftime("%Y-%m-%d %H:%M:%S")))
    print("Script duration: {0}".format(end_time - start_time))
    cursor.execute("TRUNCATE {0} ".format(new_table))
    raise e
cursor.close()
