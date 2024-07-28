#!/usr/bin/env python

import json
from datetime import datetime
from subprocess import run

# Get pull requests from github and make a changelog based on that.
# You need github cli (gh) installed.

time_format = "%Y-%m-%dT%H:%M:%S.0000000%:z"
template = """
- author: {author}
  changes:
  - message: {message}
    type: TYPE
  id: {id}
  time: '{time}'
  url: {url}"""


process = run(
    [
        "gh",
        "pr",
        "list",
        "--limit", "100",
        "--state", "merged",
        "--json", "author,title,url,mergedAt",
    ],
    capture_output=True,
)
prs = json.loads(process.stdout)
# print(prs)

with open("Resources/Changelog/Imp.yml", "a+") as file:
    for i, pull in enumerate(reversed(prs)):
        author = pull["author"]["name"]
        message = pull["title"]
        merged_at = pull["mergedAt"]
        time = datetime.fromisoformat(merged_at).strftime(time_format)
        url = pull["url"]

        entry = template.format(
            author=author,
            message=message,
            id=i,
            time=time,
            url=url,
        )

        print(entry)
        file.write(entry)
