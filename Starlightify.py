#!/usr/bin/env python3
import os
import shutil

root = "."
old_name = "_Impstation"
new_name = "_Starlight"

def merge_dirs(src, dst):
    """Recursively merge src into dst, then remove src."""
    for item in os.listdir(src):
        s = os.path.join(src, item)
        d = os.path.join(dst, item)
        if os.path.isdir(s):
            if not os.path.exists(d):
                shutil.move(s, d)
            else:
                merge_dirs(s, d)
        else:
            if os.path.exists(d):
                print(f"Overwriting file: {d}")
                os.remove(d)
            shutil.move(s, d)
    os.rmdir(src)

for dirpath, dirnames, filenames in os.walk(root, topdown=False):
    for d in dirnames:
        if d == old_name:
            old_path = os.path.join(dirpath, d)
            new_path = os.path.join(dirpath, new_name)
            print(f"Processing: {old_path} -> {new_path}")
            if os.path.exists(new_path):
                merge_dirs(old_path, new_path)
            else:
                shutil.move(old_path, new_path)
