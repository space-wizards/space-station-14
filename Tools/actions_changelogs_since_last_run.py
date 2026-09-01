#!/usr/bin/env python3

"""
Sends updates to a Discord webhook for new changelog entries since the last GitHub Actions publish run.

Automatically figures out the last run and changelog contents with the GitHub API.
"""

import itertools
import os
import sys
import urllib.parse
from pathlib import Path
from typing import Any, Iterable

import requests
import yaml
import time

import actions_changelog_github

DEBUG = os.environ.get("SS14_CHANGELOG_DEBUG", "").lower() in {"1", "true", "yes"}
DEBUG_CHANGELOG_FILE_OLD = Path("Resources/Changelog/Old.yml")
DEBUG_DISCORD_DUMP_FILE = Path("Resources/Changelog/DiscordDebug.md")

# https://discord.com/developers/docs/resources/webhook
DISCORD_SPLIT_LIMIT = 2000
DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")
TRUNCATION_SUFFIX = " [...]"

CHANGELOG_FILE = "Resources/Changelog/Changelog.yml"

TYPES_TO_EMOJI = {"Fix": "🐛", "Add": "🆕", "Remove": "❌", "Tweak": "⚒️"}

EXPERIMENTAL_LABEL = "Intent: Experimental"
EXPERIMENTAL_EMOJI = "🧪"

ChangelogEntry = dict[str, Any]


def main():
    if not DEBUG and not DISCORD_WEBHOOK_URL:
        print("No discord webhook URL found, skipping discord send")
        return

    if DEBUG:
        # to debug this script locally, you can use
        # a separate local file as the old changelog
        last_changelog_stream = DEBUG_CHANGELOG_FILE_OLD.read_text(encoding="utf-8-sig")
    else:
        # when running this normally in a GitHub actions workflow,
        # it will get the old changelog from the GitHub API
        last_changelog_stream = get_last_changelog()

    last_changelog = yaml.safe_load(last_changelog_stream)
    with open(CHANGELOG_FILE, "r", encoding="utf-8-sig") as f:
        cur_changelog = yaml.safe_load(f)

    diff = diff_changelog(last_changelog, cur_changelog)
    message_lines = changelog_entries_to_message_lines(diff)

    if DEBUG:
        dump_debug_markdown(message_lines)
        return

    send_message_lines(message_lines)


def get_last_changelog() -> str:
    github_repository = os.environ["GITHUB_REPOSITORY"]
    github_run = os.environ["GITHUB_RUN_ID"]
    github_token = os.environ["GITHUB_TOKEN"]

    session = actions_changelog_github.make_github_session(github_token)

    # If a previous workflow step already computed the last successful publish's
    # SHA, reuse it instead of querying the GitHub Actions API for the same info.
    last_sha = os.environ.get("LAST_PUBLISH_SHA")
    if last_sha:
        print(f"Using last publish SHA from environment: {last_sha}")
    else:
        most_recent = actions_changelog_github.get_most_recent_workflow(
            session, github_repository, github_run
        )
        last_sha = most_recent["head_commit"]["id"]
        print(f"Last successful publish job was {most_recent['id']}: {last_sha}")

    return get_last_changelog_by_sha(session, last_sha, github_repository)


def get_last_changelog_by_sha(
    sess: requests.Session, sha: str, github_repository: str
) -> str:
    """
    Use GitHub API to get the previous version of the changelog YAML (Actions builds are fetched with a shallow clone)
    """
    params = {
        "ref": sha,
    }
    headers = {"Accept": "application/vnd.github.raw"}

    resp = sess.get(
        f"{actions_changelog_github.GITHUB_API_URL}/repos/{github_repository}/contents/{CHANGELOG_FILE}",
        headers=headers,
        params=params,
    )
    resp.raise_for_status()
    return resp.text


def diff_changelog(
    old: dict[str, Any], cur: dict[str, Any]
) -> Iterable[ChangelogEntry]:
    """
    Find all new entries not present in the previous publish.
    """
    old_entry_ids = {e["id"] for e in old["Entries"]}
    return (e for e in cur["Entries"] if e["id"] not in old_entry_ids)


def get_discord_body(content: str):
    return {
        "content": content,
        # Do not allow any mentions.
        "allowed_mentions": {"parse": []},
        # SUPPRESS_EMBEDS
        "flags": 1 << 2,
    }


def send_discord_webhook(lines: list[str]):
    content = "".join(lines)
    body = get_discord_body(content)
    retry_attempt = 0

    try:
        response = requests.post(DISCORD_WEBHOOK_URL, json=body, timeout=10)
        while response.status_code == 429:
            retry_attempt += 1
            if retry_attempt > 20:
                print("Too many retries on a single request despite following retry_after header... giving up")
                exit(1)
            retry_after = response.json().get("retry_after", 5)
            print(f"Rate limited, retrying after {retry_after} seconds")
            time.sleep(retry_after)
            response = requests.post(DISCORD_WEBHOOK_URL, json=body, timeout=10)
        response.raise_for_status()
    except requests.exceptions.RequestException as e:
        print(f"Failed to send message: {e}")
        exit(1)


def truncate_to_limit(text: str, limit: int) -> str:
    if len(text) <= limit:
        return text

    if limit <= len(TRUNCATION_SUFFIX):
        return TRUNCATION_SUFFIX[:limit]

    return text[: limit - len(TRUNCATION_SUFFIX)].rstrip() + TRUNCATION_SUFFIX


def create_change_line(emoji: str, message: str, url: str | None) -> str:
    if url is None:
        prefix = f"{emoji} - "
        suffix = "\n"
    else:
        pr_number = urllib.parse.urlparse(url).path.rstrip("/").split("/")[-1]
        prefix = f"{emoji} - "
        suffix = f" ([#{pr_number}]({url}))\n"

    available_message_length = DISCORD_SPLIT_LIMIT - len(prefix) - len(suffix)
    if available_message_length < 1:
        raise ValueError(f"Rendered changelog line has no room for a message: {url}")

    message = truncate_to_limit(message, available_message_length)
    return f"{prefix}{message}{suffix}"


def changelog_entries_to_message_lines(entries: Iterable[ChangelogEntry]) -> list[str]:
    """Process structured changelog entries into a list of lines making up a formatted message."""
    message_lines = []

    for contributor_name, group in itertools.groupby(entries, lambda x: x["author"]):
        message_lines.append("\n")
        message_lines.append(f"**{contributor_name}** updated:\n")

        for entry in group:
            url = entry.get("url")
            if url and not url.strip():
                url = None

            for change in entry["changes"]:
                emoji = TYPES_TO_EMOJI.get(change["type"], "❓")
                message = change["message"]

                labels = entry.get("labels") or []
                if EXPERIMENTAL_LABEL in labels:
                    emoji = f"{emoji}{EXPERIMENTAL_EMOJI}"

                message_lines.append(create_change_line(emoji, message, url))

    return message_lines


def split_message_lines(message_lines: list[str]) -> list[list[str]]:
    """Join message lines into chunks that are each below Discord's message length limit."""
    chunks = []
    chunk_lines = []
    chunk_length = 0

    for line in message_lines:
        line_length = len(line)
        if line_length > DISCORD_SPLIT_LIMIT:
            raise ValueError(
                f"Changelog line is too long for Discord after truncation: {line_length}"
            )

        new_chunk_length = chunk_length + line_length

        if new_chunk_length > DISCORD_SPLIT_LIMIT:
            if chunk_lines:
                chunks.append(chunk_lines)

            new_chunk_length = line_length
            chunk_lines = []

        chunk_lines.append(line)
        chunk_length = new_chunk_length

    if chunk_lines:
        chunks.append(chunk_lines)

    return chunks


def dump_debug_markdown(message_lines: list[str]):
    chunks = split_message_lines(message_lines)

    with DEBUG_DISCORD_DUMP_FILE.open("w", encoding="utf-8", newline="\n") as f:
        f.write("# Discord Changelog Debug Dump\n\n")
        f.write(
            f"Generated from `{DEBUG_CHANGELOG_FILE_OLD}` to `{CHANGELOG_FILE}`.\n\n"
        )

        if not chunks:
            f.write("_No changelog entries to send._\n")
            return

        for i, chunk_lines in enumerate(chunks, start=1):
            content = "".join(chunk_lines)
            f.write(
                f"<!-- Discord message break: chunk {i}/{len(chunks)}, {len(content)}/{DISCORD_SPLIT_LIMIT} characters -->\n\n"
            )
            f.write(f"## Discord Message {i}\n\n")
            f.write(content.lstrip("\n"))
            f.write("\n")

    print(f"Wrote Discord changelog debug dump to {DEBUG_DISCORD_DUMP_FILE}")


def send_message_lines(message_lines: list[str]):
    """Join a list of message lines into chunks that are each below Discord's message length limit, and send them."""
    chunks = split_message_lines(message_lines)

    for chunk_lines in chunks[:-1]:
        print("Split changelog and sending to discord")
        send_discord_webhook(chunk_lines)

    if chunks:
        print("Sending final changelog to discord")
        send_discord_webhook(chunks[-1])


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"Failed to publish changelog to Discord: {e}", file=sys.stderr)
        exit(1)
