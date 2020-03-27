
# Unquoted List Association Locater and Reporter, by RemieRichards V1.0 - 26/11/16
# list("string" = value) is valid in DM
# unfortunately, so is list(string = value) (Notice the lack of quotes? BAD BAD, it conflicts with var names!)

import sys
import os
import re
from datetime import date


#Climbs up from /tools/UnquotedListAssociations and along to ../code
scan_dir = "code" #used later to truncate log file paths
real_dir = os.path.abspath("../../"+scan_dir)
define_dict = {}
total_unquoted_list_associations = 0
log_output = True #Set to false for mad speeeeeed (slightly faster because no text output to the window, but still full log files)


#Scan a directory, scanning any dm files it finds
def unquoted_list_associations_scan_dir(scan_dir):
    global total_unquoted_list_associations
    if os.path.exists(scan_dir):
        if os.path.isdir(scan_dir):
            build_define_dictionary(scan_dir)

            output_str = ""

            files_scanned = 0
            files_with_named_list_args = 0
            for root, dirs, files in os.walk(scan_dir):
                for f in files:
                    if log_output:
                        print str(f)
                    scan_result = scan_dm_file_for_unquoted_list_associations(os.path.join(root, f))
                    files_scanned += 1
                    if scan_result:
                        output_str += scan_result+"\n"
                        files_with_named_list_args += 1
            output_str += str(files_with_named_list_args) + "/" + str(files_scanned) + " files have Unquoted List Associations in them"
            output_str += "\nThere are " + str(total_unquoted_list_associations) + " total Unquoted List Associations"

            todays_file = str(date.today())+"-unquoted_list_associations_log.txt"
            output_file = open(todays_file, "w") #w so it overrides existing files for today, there should only really be one file per day
            output_file.write(output_str)


#Scan one file, returning a string as a "report" or if there are no NamedListArgs, False
def scan_dm_file_for_unquoted_list_associations(_file):
    global total_unquoted_list_associations
    if not _file.endswith(".dm"):
        return False
    
    with open(_file, "r") as dm_file:
        filecontents = dm_file.read()

        unquoted_list_associations = []
        list_definitions = []

        for listdef in re.findall(r"=\s*list\((.*)\)", filecontents):
            list_definitions.append(listdef)

        listdefs = ' '.join(list_definitions)

        for matchtuple in re.findall(r"(?:list\(|,)\s*(\w+)\s*,*\s*=\s*(\w+)", listdefs):
            if not define_dict.get(matchtuple[0], False): #defines are valid
                unquoted_list_associations.append(matchtuple)
                
        count = len(unquoted_list_associations)
            
        if count:
            file_report = ".."+scan_dir+str(_file).split(scan_dir)[1]+" " #crop it down to ..\code\DIR\FILE.dm, everything else is developer specific
            for nla in unquoted_list_associations:
                file_report += "\nlist(" + nla[0] + " = " + nla[1] + ")"
            total_unquoted_list_associations += count
            file_report += "\nTotal Unquoted List Associations: "+str(count)
            return file_report
        else:
            return False

#Build a dict of defines, such that we can rule them out as NamedListArgs
def build_define_dictionary(scan_dir):
    define_dict = {}
    for root, dirs, files in os.walk(scan_dir):
        for f in files:
            scan_dm_file_for_defines(os.path.join(root, f))


#Find all #define X Y in a file and update define_dict so that define_dict[X] = True
def scan_dm_file_for_defines(_file):
    if not _file.endswith(".dm"):
        return False

    with open(_file, "r") as dm_file:
        filecontents = dm_file.read()

        for define_def in re.findall(r"#define\s+([\w()]+)[ \t]+[^\n]+", filecontents):
            define_dict[define_def] = True

        
unquoted_list_associations_scan_dir(real_dir)
print "Done!"
