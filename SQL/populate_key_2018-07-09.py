#Python 3+ Script for populating the key of all ckeys in player table made by Jordie0608
#
#Before starting ensure you have installed the mysqlclient package https://github.com/PyMySQL/mysqlclient-python
#It can be downloaded from command line with pip:
#pip install mysqlclient
#And that you have run the most recent commands listed in database_changelog.txt
#
#To view the parameters for this script, execute it with the argument --help
#All the positional arguments are required, remember to include a prefixe in your table name if you use one
#--useckey and --onlynull are optional arguments, see --help for their function
#An example of the command used to execute this script from powershell:
#python populate_key_2018-07-09.py "localhost" "root" "password" "feedback" "SS13_player" --onlynull --useckey
#
#This script requires an internet connection to function
#Sometimes byond.com fails to return the page for a valid ckey, this can be a temporary problem and may be resolved by rerunning the script
#You can have the script use the existing ckey instead if the key is unable to be parsed with the --useckey optional argument
#To make the script only iterate on rows that failed to parse a ckey and have a null byond_key column, use the --onlynull optional argument
#To make the script only iterate on rows that have a matching ckey and byond_key column, use the --onlyckeymatch optional argument
#The --onlynull and --onlyckeymatch arguments are mutually exclusive, the script can't be run with both of them enabled
#
#It's safe to run this script with your game server(s) active.

import MySQLdb
import argparse
import re
import sys
from urllib.request import urlopen
from datetime import datetime

if sys.version_info[0] < 3:
    raise Exception("Python must be at least version 3 for this script.")
query_values = ""
current_round = 0
parser = argparse.ArgumentParser()
parser.add_argument("address", help="MySQL server address (use localhost for the current computer)")
parser.add_argument("username", help="MySQL login username")
parser.add_argument("password", help="MySQL login password")
parser.add_argument("database", help="Database name")
parser.add_argument("playertable", help="Name of the player table (remember a prefix if you use one)")
parser.add_argument("--useckey", help="Use the player's ckey for their key if unable to contact or parse their member page", action="store_true")
group = parser.add_mutually_exclusive_group()
group.add_argument("--onlynull", help="Only try to update rows if their byond_key column is null, mutually exclusive with --onlyckeymatch", action="store_true")
group.add_argument("--onlyckeymatch", help="Only try to update rows that have matching ckey and byond_key columns from the --useckey argument, mutually exclusive with --onlynull", action="store_true")
args = parser.parse_args()
where = ""
if args.onlynull:
    where = " WHERE byond_key IS NULL"
if args.onlyckeymatch:
    where = " WHERE byond_key = ckey"
db=MySQLdb.connect(host=args.address, user=args.username, passwd=args.password, db=args.database)
cursor=db.cursor()
player_table = args.playertable
cursor.execute("SELECT ckey FROM {0}{1}".format(player_table, where))
ckey_list = cursor.fetchall()
failed_ckeys = []
start_time = datetime.now()
success = 0
fail = 0
print("Beginning script at {0}".format(start_time.strftime("%Y-%m-%d %H:%M:%S")))
if not ckey_list:
    print("Query returned no rows")
for current_ckey in ckey_list:
    link = urlopen("https://secure.byond.com/members/{0}/?format=text".format(current_ckey[0]))
    data = link.read()
    data = data.decode("ISO-8859-1")
    match = re.search("\tkey = \"(.+)\"", data)
    if match:
        key = match.group(1)
        success += 1
    else:
        fail += 1
        failed_ckeys.append(current_ckey[0])
        msg = "Failed to parse a key for {0}".format(current_ckey[0])
        if args.useckey:
            msg += ", using their ckey instead"
            print(msg)
            key = current_ckey[0]
        else:
            print(msg)
            continue
    cursor.execute("UPDATE {0} SET byond_key = %s WHERE ckey = %s".format(player_table), (key, current_ckey[0]))
    db.commit()
end_time = datetime.now()
print("Script completed at {0} with duration {1}".format(datetime.now().strftime("%Y-%m-%d %H:%M:%S"), end_time - start_time))
if failed_ckeys:
    if args.useckey:
        print("The following ckeys failed to parse a key so their ckey was used instead:")
    else:
        print("The following ckeys failed to parse a key and were skipped:")
    print("\n".join(failed_ckeys))
    print("Keys successfully parsed: {0}    Keys failed parsing: {1}".format(success, fail))
cursor.close()
