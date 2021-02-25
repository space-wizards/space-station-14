#!/usr/bin/env python3

import sys
import os
from typing import List, Any
import yaml
import argparse
import datetime

MAX_ENTRIES = 500

HEADER_RE = r"(?::cl:|🆑) *\r?\n(.+)$"
ENTRY_RE = r"^ *[*-]? *(\S[^\n\r]+)\r?$"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("changelog_file")
    parser.add_argument("parts_dir")

    args = parser.parse_args()

    with open(args.changelog_file, "r", encoding="utf-8-sig") as f:
        current_data = yaml.safe_load(f)

    entries_list: List[Any]
    if current_data is None:
        entries_list = []
    else:
        entries_list = current_data["Entries"]

    max_id = max(map(lambda e: e["id"], entries_list), default=0)

    for partname in os.listdir(args.parts_dir):
        if not partname.endswith(".yml"):
            continue

        partpath = os.path.join(args.parts_dir, partname)
        print(partpath)

        partyaml = yaml.safe_load(open(partpath, "r", encoding="utf-8-sig"))

        author = partyaml["author"]
        time = partyaml.get(
            "time", datetime.datetime.now(datetime.timezone.utc).isoformat()
        )
        changes = partyaml["changes"]
        max_id += 1
        new_id = max_id

        entries_list.append(
            {"author": author, "time": time, "changes": changes, "id": new_id}
        )

        os.remove(partpath)

    print(f"Have {len(entries_list)} changelog entries")

    entries_list.sort(key=lambda e: e["id"])

    overflow = len(entries_list) - MAX_ENTRIES
    if overflow > 0:
        print(f"Removing {overflow} old entries.")
        entries_list = entries_list[overflow:]

    with open(args.changelog_file, "w") as f:
        yaml.safe_dump({"Entries": entries_list}, f)


main()
