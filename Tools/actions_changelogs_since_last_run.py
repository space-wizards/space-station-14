#!/usr/bin/env python3

"""
Sends updates to a Discord webhook for new changelog entries since the last GitHub Actions publish run.

Automatically figures out the last run and changelog contents with the GitHub API.
"""

import itertools
import os
from pathlib import Path
from typing import Any, Iterable

import requests
import yaml
import time

DEBUG = False
DEBUG_CHANGELOG_FILE_OLD = Path("Resources/Changelog/Old.yml")
GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")

DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")
DISCORD_CHANGELOG_ROLE_ID = 1308143973684088883

CHANGELOG_FILE = "Resources/Changelog/ChangelogStarlight.yml"
TYPES_TO_EMOJI = {"Fix": "🐛", "Add": "🆕", "Remove": "❌", "Tweak": "⚒️"}
ChangelogEntry = dict[str, Any]

EMBED_DESCRIPTION_LIMIT = 4096
EMBED_TITLE_LIMIT = 256
EMBED_FIELD_NAME_LIMIT = 256
EMBED_FIELD_VALUE_LIMIT = 1024


def main():
    if not DISCORD_WEBHOOK_URL:
        print("No webhook URL; skipping send")
        return

    if DEBUG:
        last_changelog_stream = DEBUG_CHANGELOG_FILE_OLD.read_text()
    else:
        last_changelog_stream = get_last_changelog()

    last_changelog = yaml.safe_load(last_changelog_stream) or {}
    with open(CHANGELOG_FILE, "r") as f:
        cur_changelog = yaml.safe_load(f) or {}

    new_entries = list(diff_changelog(last_changelog, cur_changelog))
    if not new_entries:
        print("No new entries to report.")
        return

    ping_role_once(DISCORD_CHANGELOG_ROLE_ID)

    pr_groups = group_entries_by_pr(new_entries)
    for pr_id, entries in pr_groups.items():
        embed = build_embed_for_pr(pr_id, entries)
        send_embed(embed)


def get_most_recent_workflow(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    current = get_current_run(sess, github_repository, github_run)
    past = get_past_runs(sess, current)
    runs = past.get("workflow_runs", [])
    # sort descending by creation timestamp to pick the latest successful before current
    sorted_runs = sorted(runs, key=lambda r: r["created_at"], reverse=True)
    for run in sorted_runs:
        if run["id"] == current["id"]:
            continue
        return run
    raise RuntimeError("No previous successful workflow run found")


def get_current_run(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    resp = sess.get(f"{GITHUB_API_URL}/repos/{github_repository}/actions/runs/{github_run}")
    resp.raise_for_status()
    return resp.json()


def get_past_runs(sess: requests.Session, current_run: Any) -> Any:
    params = {"status": "success", "created": f"<={current_run['created_at']}"}
    resp = sess.get(f"{current_run['workflow_url']}/runs", params=params)
    resp.raise_for_status()
    return resp.json()


def get_last_changelog() -> str:
    github_repository = os.environ["GITHUB_REPOSITORY"]
    github_run = os.environ["GITHUB_RUN_ID"]
    github_token = os.environ["GITHUB_TOKEN"]

    session = requests.Session()
    session.headers["Authorization"] = f"Bearer {github_token}"
    session.headers["Accept"] = "Accept: application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"

    most_recent = get_most_recent_workflow(session, github_repository, github_run)
    last_sha = most_recent["head_commit"]["id"]
    print(f"Last successful publish job was {most_recent['id']}: {last_sha}")
    return get_last_changelog_by_sha(session, last_sha, github_repository)


def get_last_changelog_by_sha(
    sess: requests.Session, sha: str, github_repository: str
) -> str:
    params = {"ref": sha}
    headers = {"Accept": "application/vnd.github.raw"}
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/contents/{CHANGELOG_FILE}",
        headers=headers,
        params=params,
    )
    resp.raise_for_status()
    return resp.text


def diff_changelog(old: dict[str, Any], cur: dict[str, Any]) -> Iterable[ChangelogEntry]:
    old_ids = {e["id"] for e in old.get("Entries", [])}
    return (e for e in cur.get("Entries", []) if e["id"] not in old_ids)


def group_entries_by_pr(entries: Iterable[ChangelogEntry]) -> dict[str, list[ChangelogEntry]]:
    groups: dict[str, list[ChangelogEntry]] = {}
    for entry in entries:
        url = entry.get("url", "")
        if url and url.strip():
            pr_number = url.rstrip("/").split("/")[-1]
        else:
            pr_number = "no-pr"
        groups.setdefault(pr_number, []).append(entry)
    return groups


def build_embed_for_pr(pr_id: str, entries: list[ChangelogEntry]) -> dict[str, Any]:
    authors = set()
    description_lines: list[str] = []

    for entry in entries:
        authors.add(entry.get("author", "Unknown"))
        url = entry.get("url", "").strip() or None
        for change in entry.get("changes", []):
            emoji = TYPES_TO_EMOJI.get(change.get("type", ""), "❓")
            message = change.get("message", "").strip()
            if len(message) > 300:
                message = message[:297].rstrip() + "..."
            line = f"{emoji} {message}"
            if url and pr_id != "no-pr":
                line += f" ([#{pr_id}]({url}))"
            description_lines.append(line)

    description = "\n".join(description_lines)
    if len(description) > EMBED_DESCRIPTION_LIMIT:
        description = description[: EMBED_DESCRIPTION_LIMIT - 50].rstrip() + "\n*...truncated...*"

    sorted_authors = sorted(authors)
    authors_str = ", ".join(sorted_authors)
    title = authors_str
    if len(title) > EMBED_TITLE_LIMIT:
        # truncate authors part to fit
        overflow = len(title) - EMBED_TITLE_LIMIT + 3  # for "..."
        # remove overflow chars from authors_str
        truncated_authors = authors_str
        if overflow < len(authors_str):
            truncated_authors = authors_str[: -overflow].rstrip()
            # avoid cutting mid-comma: optionally rstrip to last comma-space
            if "," in truncated_authors:
                truncated_authors = truncated_authors.rsplit(",", 1)[0]
            truncated_authors = truncated_authors.rstrip() + "..."
        title = truncated_authors
        if len(title) > EMBED_TITLE_LIMIT:
            title = title[:EMBED_TITLE_LIMIT]

    author_field = ", ".join(sorted_authors)
    embed: dict[str, Any] = {
        "title": title,
        "description": description,
        "fields": [
            {"name": "Author(s)", "value": author_field[:EMBED_FIELD_VALUE_LIMIT], "inline": False}
        ],
        "footer": {"text": "Starlight changelog"},
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
    }
    if pr_id != "no-pr":
        embed["url"] = entries[0].get("url", "")
    return embed


def send_embed(embed: dict[str, Any]):
    payload = {
        "embeds": [embed],
        "allowed_mentions": {"parse": []},  # no automatic pings
    }
    post_with_retries(payload)


def ping_role_once(role_id: str):
    content = f"<@&{role_id}> New changelog updates are ready for release."
    payload = {
        "content": content,
        "allowed_mentions": {"roles": [int(role_id)]},
    }
    post_with_retries(payload)


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

                # if a single line is longer than the limit, it needs to be truncated
                if len(message) > DISCORD_SPLIT_LIMIT:
                    message = message[: DISCORD_SPLIT_LIMIT - 100].rstrip() + " [...]"

                if url is not None:
                    pr_number = url.split("/")[-1]
                    line = f"{emoji} - {message} ([#{pr_number}]({url}))\n"
                else:
                    line = f"{emoji} - {message}\n"

                message_lines.append(line)

    return message_lines


def send_message_lines(message_lines: list[str]):
    """Join a list of message lines into chunks that are each below Discord's message length limit, and send them."""
    chunk_lines = []
    chunk_length = 0

    for line in message_lines:
        line_length = len(line)
        new_chunk_length = chunk_length + line_length

        if new_chunk_length > DISCORD_SPLIT_LIMIT:
            print("Split changelog and sending to discord")
            send_discord_webhook(chunk_lines)

            new_chunk_length = line_length
            chunk_lines.clear()

        chunk_lines.append(line)
        chunk_length = new_chunk_length

    if chunk_lines:
        print("Sending final changelog to discord")
        send_discord_webhook(chunk_lines)


if __name__ == "__main__":
    main()
