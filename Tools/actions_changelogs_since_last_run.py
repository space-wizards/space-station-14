#!/usr/bin/env python3

"""
Sends updates to a Discord webhook for new changelog entries since the last GitHub Actions publish run.

Automatically figures out the last run and changelog contents with the GitHub API.
"""

import io
import itertools
import os
from pathlib import Path
from typing import Any, Iterable

import requests
import yaml

DEBUG = True
DEBUG_CHANGELOG_FILE_OLD = Path("../Changelog-Impstation-old.yml")
GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")

# https://discord.com/developers/docs/resources/webhook
DISCORD_SPLIT_LIMIT = 2000
DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")

CHANGELOG_FILE = "Resources/Changelog/Impstation.yml"

TYPES_TO_EMOJI = {"Fix": "🐛", "Add": "🆕", "Remove": "❌", "Tweak": "⚒️"}

ChangelogEntry = dict[str, Any]


def main():
    if not DISCORD_WEBHOOK_URL:
        print("Discord webhook not set, skipping")
        return

    if DEBUG:
        last_changelog_stream = DEBUG_CHANGELOG_FILE_OLD.read_text()
    else:
        last_changelog_stream = get_last_changelog()

    last_changelog = yaml.safe_load(last_changelog_stream)
    with open(CHANGELOG_FILE, "r") as f:
        cur_changelog = yaml.safe_load(f)

    diff = diff_changelog(last_changelog, cur_changelog)
    send_entries(diff)


def get_most_recent_workflow(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    workflow_run = get_current_run(sess, github_repository, github_run)
    past_runs = get_past_runs(sess, workflow_run)
    for run in past_runs["workflow_runs"]:
        # First past successful run that isn't our current run.
        if run["id"] == workflow_run["id"]:
            continue

        return run


def get_current_run(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/actions/runs/{github_run}"
    )
    resp.raise_for_status()
    return resp.json()


def get_past_runs(sess: requests.Session, current_run: Any) -> Any:
    """
    Get all successful workflow runs before our current one.
    """
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
    last_changelog_stream = get_last_changelog_by_sha(
        session, last_sha, github_repository
    )

    return last_changelog_stream


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
        f"{GITHUB_API_URL}/repos/{github_repository}/contents/{CHANGELOG_FILE}",
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


def send_discord_webhook(content: str):
    body = get_discord_body(content)

    response = requests.post(DISCORD_WEBHOOK_URL, json=body)
    response.raise_for_status()


def format_entry_line(url: str, change: ChangelogEntry) -> str:
    emoji = TYPES_TO_EMOJI.get(change["type"], "❓")
    message = change["message"]
    if url is not None:
        line = f"{emoji} - {message} [PR]({url}) \n"
    else:
        line = f"{emoji} - {message}\n"

    return line


def send_entries(entries: Iterable[ChangelogEntry]) -> None:
    if not DISCORD_WEBHOOK_URL:
        print("No discord webhook URL found, skipping discord send")
        return

    message_content = io.StringIO()
    # We need to manually split messages to avoid discord's character limit
    # With that being said this isn't entirely robust
    # e.g. a sufficiently large CL breaks it, but that's a future problem

    for name, group in itertools.groupby(entries, lambda x: x["author"]):
        # Need to split text to avoid discord character limit
        group_content = io.StringIO()
        group_content.write(f"**{name}** updated:\n")

        for entry in group:
            url = entry.get("url")
            if url and not url.strip():
                url = None
            for change in entry["changes"]:
                group_content.write(format_entry_line(url, change))

        group_text = group_content.getvalue()
        message_text = message_content.getvalue()
        message_length = len(message_text)
        group_length = len(group_text)

        # If adding the text would bring it over the group limit then send the message and start a new one
        if message_length + group_length >= DISCORD_SPLIT_LIMIT:
            print("Split changelog  and sending to discord")
            send_discord_webhook(message_text)

            # Reset the message
            message_content = io.StringIO()

        # Flush the group to the message
        message_content.write(group_text)

    # Clean up anything remaining
    message_text = message_content.getvalue()
    if len(message_text) > 0:
        print("Sending final changelog to discord")
        send_discord_webhook(message_text)


if __name__ == "__main__":
    main()
