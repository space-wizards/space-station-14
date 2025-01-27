#!/usr/bin/env python

import json
from datetime import datetime
from subprocess import run

# Get pull requests from github and make a changelog based on that.
# You need github cli (gh) installed.

file_path = "Resources/Changelog/Impstation-new.yml"
time_format = "%Y-%m-%dT%H:%M:%S.0000000%:z"
template = """
- author: {author}
  changes:
  - message: {message}
    type: {type}
  id: {id}
  time: '{time}'
  url: {url}"""


process = run(
    [
        "gh",
        "pr",
        "list",
        "--limit", "500",
        "--state", "merged",
        "--json", "author,title,url,mergedAt",
    ],
    capture_output=True,
)
prs = json.loads(process.stdout)
# print(prs)


def get_entry_type(pull_message: str) -> str:
    types = {
        "a": "Add",
        "r": "Remove",
        "t": "Tweak",
        "f": "Fix",
    }
    choice = input(f"Type for this entry: '{pull_message}'\n")
    result = types.get(choice)
    return result


with open(file_path, "a+") as file:
    for i, pull in enumerate(reversed(prs)):
        author = pull["author"]["login"]
        message = pull["title"]
        # entry_type = get_entry_type(message)
        entry_type = "None"
        merged_at = pull["mergedAt"]
        time = datetime.fromisoformat(merged_at).strftime(time_format)
        url = pull["url"]

        entry = template.format(
            author=author,
            message=message,
            type=entry_type,
            id=i,
            time=time,
            url=url,
        )

        print(entry)
        file.write(entry)
