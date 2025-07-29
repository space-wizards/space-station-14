#!/usr/bin/env python3

import io
import itertools
import os
import requests
import yaml
from typing import Any, Iterable
from datetime import datetime
import time

# Discord and GitHub settings
DISCORD_WEBHOOK_URL = os.getenv("DISCORD_WEBHOOK_URL")
CHANGELOG_FILE = "Resources/Changelog/ChangelogStarlight.yml"
SENT_IDS_FILE = "Tools/changelogs/sent_changelog_ids.yml"

TYPES_TO_EMOJI = {
    "Fix":    "🐛",
    "Add":    "🆕",
    "Remove": "❌",
    "Tweak":  "⚒️"
}

ChangelogEntry = dict[str, Any]

def main():
    if not DISCORD_WEBHOOK_URL:
        print("Bad Webhook URL")
        return

    with open(CHANGELOG_FILE, "r") as f:
        cur_changelog = yaml.safe_load(f)

    sent_ids = load_sent_ids(SENT_IDS_FILE)
    new_entries = diff_changelog(sent_ids, cur_changelog)
    
    if new_entries:
        send_to_discord(new_entries)
        update_sent_ids(SENT_IDS_FILE, new_entries)
    else:
        print("No new changelog entries to send.")


def load_sent_ids(filename: str) -> set[str]:
    try:
        with open(filename, "r") as f:
            return set(yaml.safe_load(f) or [])
    except FileNotFoundError:
        return set()


def update_sent_ids(filename: str, entries: Iterable[ChangelogEntry]) -> None:
    sent_ids = load_sent_ids(filename)
    sent_ids.update(entry["id"] for entry in entries)

    with open(filename, "w") as f:
        yaml.safe_dump(list(sent_ids), f)


def diff_changelog(sent_ids: set[str], cur: dict[str, Any]) -> Iterable[ChangelogEntry]:
    return (e for e in cur["Entries"] if e["id"] not in sent_ids)


def send_to_discord(entries: Iterable[ChangelogEntry]) -> None:
    entries = sorted(entries, key=lambda x: (x["author"], x["time"]))
    sent_ids = []

    for author, group in itertools.groupby(entries, key=lambda x: x["author"]):
        group = list(group)
        try:
            entry_time = datetime.strptime(group[0]["time"], "%Y-%m-%dT%H:%M:%S.%f%z")
        except ValueError:
            print(f"Invalid time format for entry by {author}: {group[0]['time']}")
            continue

        embed = {
            "title": author,
            "description": "",
            "fields": [],
            "timestamp": entry_time.isoformat(),
            "color": 0x7289DA,
        }

        changes_by_type = {}
        urls = set()

        for entry in group:
            sent_ids.append(entry["id"])
            for change in entry["changes"]:
                emoji = TYPES_TO_EMOJI.get(change['type'], "❓")
                url = entry.get("url")

                message = change["message"]
                if emoji not in changes_by_type:
                    changes_by_type[emoji] = []

                # if a single line is longer than the limit, it needs to be truncated
                if len(message) > DISCORD_SPLIT_LIMIT:
                    message = message[: DISCORD_SPLIT_LIMIT - 100].rstrip() + " [...]"

                if url is not None:
                    pr_number = url.split("/")[-1]
                    line = f"{emoji} - {message} ([#{pr_number}]({url}))\n"
                else:
                    line = f"{emoji} - {message}\n"

        for emoji, messages in changes_by_type.items():
            for message in messages:
                embed["description"] += f"\n {emoji} {message}"
                
        if urls:
            embed["description"] += "\n\nRelated Pull Requests:\n" + "\n".join(f"- [GitHub Pull Request]({url})" for url in urls)

        send_discord(embed)
        time.sleep(0.5)

    update_sent_ids(SENT_IDS_FILE, [{"id": eid} for eid in sent_ids])




def send_discord(embed: dict):
    response = requests.post(DISCORD_WEBHOOK_URL, json={"content": "<@&1308143973684088883>", "embeds": [embed]})
    response.raise_for_status()


main()
