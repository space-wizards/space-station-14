#!/usr/bin/env python

import re, os, sys, fnmatch


# Regex pattern to extract the directory path in a #define FILE_DIR
filedir_pattern = re.compile(r'^#define\s*FILE_DIR\s*"(.*?)"')

# Regex pattern to extract any single quoted piece of text. This can also
# match single quoted strings inside of double quotes, which is part of a
# regular text string and should not be replaced. The replacement function
# however will any match that doesn't appear to be a filename so these
# extra matches should not be a problem.
rename_pattern = re.compile(r"'(.+?)'")

# Only filenames matching this pattern will have their resources renamed
source_pattern = re.compile(r"^.*?\.(dm|dmm)$")

# Open the .dme file and return a list of all FILE_DIR paths in it
def read_filedirs(filename):
    result = []
    dme_file = file(filename, "rt")
    
    # Read each line from the file and check for regex pattern match
    for row in dme_file:
        match = filedir_pattern.match(row)
        if match:
            result.append(match.group(1))

    dme_file.close()
    return result

# Search through a list of directories, and build a dictionary which
# maps every file to its full pathname (relative to the .dme file)
# If the same filename appears in more than one directory, the earlier
# directory in the list takes preference.
def index_files(file_dirs):
    result = {}

    # Reverse the directory list so the earlier directories take precedence
    # by replacing the previously indexed file of the same name
    for directory in reversed(file_dirs):
        for name in os.listdir(directory):
            # Replace backslash path separators on Windows with forward slash
            # Force "name" to lowercase when used as a key since BYOND resource
            # names are case insensitive, even on Linux.
            if name.find(".") == -1:
                continue
            result[name.lower()] = directory.replace('\\', '/') + '/' + name

    return result

# Recursively search for every .dm/.dmm file in the .dme file directory. For
# each file, search it for any resource names in single quotes, and replace
# them with the full path previously found by index_files()
def rewrite_sources(resources):
    # Create a closure for the regex replacement function to capture the
    # resources dictionary which can't be passed directly to this function
    def replace_func(name):
        key = name.group(1).lower()
        if key in resources:
            replacement = resources[key]
        else:
            replacement = name.group(1)
        return "'" + replacement + "'"
    
    # Search recursively for all .dm and .dmm files
    for (dirpath, dirs, files) in os.walk("."):
        for name in files:
            if source_pattern.match(name):
                path = dirpath + '/' + name
                source_file = file(path, "rt")
                output_file = file(path + ".tmp", "wt")

                # Read file one line at a time and perform replacement of all
                # single quoted resource names with the fullpath to that resource
                # file. Write the updated text back out to a temporary file.
                for row in source_file:
                    row = rename_pattern.sub(replace_func, row)
                    output_file.write(row)

                output_file.close()
                source_file.close()

                # Delete original source file and replace with the temporary
                # output. On Windows, an atomic rename() operation is not
                # possible like it is under POSIX.
                os.remove(path)
                os.rename(path + ".tmp", path)

dirs = read_filedirs("tgstation.dme");
resources = index_files(dirs)
rewrite_sources(resources)
