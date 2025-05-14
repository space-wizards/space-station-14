#!/usr/bin/env python

from pathlib import Path
from re import sub
from sys import argv

# Edit the changelog so that the ids are sequential, starting at i.

try:
    i = int(argv[1])
except IndexError:
    i = 1


file_path = Path("Resources/Changelog/Impstation.yml")
changelog = file_path.read_text()

prefix = "id: "


def get_id(_):
    global i
    result = f"{prefix}{i}"
    i += 1
    return result


changelog = sub(f"{prefix}\\d+", get_id, changelog)

file_path.write_text(changelog)
