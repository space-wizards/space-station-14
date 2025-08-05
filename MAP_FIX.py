#!/usr/bin/env python3
import re
import sys
import os

# Replace target regex matches
def transform_text(file):
    pattern = r'- - (.*?)\n {10}- (.*)'
    return re.sub(pattern, r'- \1: \2', file)

#Processes every file
def process_file(filepath):
    # Read file
    with open(filepath, 'r', encoding='utf-8') as file:
        filedata = file.read()

    #run the replace
    newdata = transform_text(filedata)

    # Write file
    with open(filepath, 'w', encoding='utf-8') as file:
        file.write(newdata)
    print(f"Processed: {filepath}")

#Processes file or processes all files
def process_path(path):
    #single file
    if os.path.isfile(path):
        #return if not .yml
        if not path.endswith('.yml'):
            return
        process_file(path)
    #folder
    elif os.path.isdir(path):
        for root, _, files in os.walk(path):
            for name in files:
                if name.endswith('.yml'):
                    process_file(os.path.join(root, name))
    else:
        print(f"Path not found: {path}")

if __name__ == "__main__":
    #check for necessary filepath
    if len(sys.argv) < 2:
        print("Usage: MAP_FIX.py <path-to-.yml-file-or-directory>")
        sys.exit(1)
    
    input_path = sys.argv[1]
    #Convert to absolute path
    script_dir = os.path.dirname(os.path.abspath(__file__))
    full_path = os.path.join(script_dir, input_path)

    process_path(full_path)