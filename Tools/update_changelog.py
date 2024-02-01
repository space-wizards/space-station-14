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

CATEGORY_MAIN = "Main"

# From https://stackoverflow.com/a/37958106/4678631
class NoDatesSafeLoader(yaml.SafeLoader):
    @classmethod
    def remove_implicit_resolver(cls, tag_to_remove):
        if not 'yaml_implicit_resolvers' in cls.__dict__:
            cls.yaml_implicit_resolvers = cls.yaml_implicit_resolvers.copy()

        for first_letter, mappings in cls.yaml_implicit_resolvers.items():
            cls.yaml_implicit_resolvers[first_letter] = [(tag, regexp)
                                                         for tag, regexp in mappings
                                                         if tag != tag_to_remove]

# Hrm yes let's make the fucking default of our serialization library to PARSE ISO-8601
# but then output garbage when re-serializing.
NoDatesSafeLoader.remove_implicit_resolver('tag:yaml.org,2002:timestamp')

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("changelog_file")
    parser.add_argument("parts_dir")
    parser.add_argument("--category", default=CATEGORY_MAIN)

    args = parser.parse_args()

    category = args.category

    with open(args.changelog_file, "r", encoding="utf-8-sig") as f:
        current_data = yaml.load(f, Loader=NoDatesSafeLoader)

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

        with open(partpath, "r", encoding="utf-8-sig") as f:
            partyaml = yaml.load(f, Loader=NoDatesSafeLoader)

        part_category = partyaml.get("category", CATEGORY_MAIN)
        if part_category != category:
            print(f"Skipping: wrong category ({part_category} vs {category})")
            continue

        author = partyaml["author"]
        time = partyaml.get(
            "time", datetime.datetime.now(datetime.timezone.utc).isoformat()
        )
        changes = partyaml["changes"]
        url = partyaml.get("url")

        if not isinstance(changes, list):
            changes = [changes]

        if len(changes):
            # Don't add empty changelog entries...
            max_id += 1
            new_id = max_id

            entries_list.append(
                {"author": author, "time": time, "changes": changes, "id": new_id, "url": url}
            )

        os.remove(partpath)

    print(f"Have {len(entries_list)} changelog entries")

    entries_list.sort(key=lambda e: e["id"])

    overflow = len(entries_list) - MAX_ENTRIES
    if overflow > 0:
        print(f"Removing {overflow} old entries.")
        entries_list = entries_list[overflow:]

    new_data = {"Entries": entries_list}
    for key, value in current_data.items():
        if key != "Entries":
            new_data[key] = value

    with open(args.changelog_file, "w", encoding="utf-8-sig") as f:
        yaml.safe_dump(new_data, f)


main()
