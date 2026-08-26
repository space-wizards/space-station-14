#!/usr/bin/env python3

"""
Computes the SHA of the last successful GitHub Actions workflow run.

This is meant to run as its own step in a workflow and write the result to
GITHUB_OUTPUT, so that other steps (changelog publishing, changelog updates,
etc.) can reuse it instead of each querying the GitHub API for the same thing.
"""

import os
import sys
from typing import Any, Iterable

import requests

GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")


def get_most_recent_workflow(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    workflow_run = get_current_run(sess, github_repository, github_run)
    past_runs = get_past_runs(sess, workflow_run)
    for run in past_runs:
        return run

    raise RuntimeError("Could not find a previous successful workflow run")


def get_current_run(
    sess: requests.Session, github_repository: str, github_run: str
) -> Any:
    resp = sess.get(
        f"{GITHUB_API_URL}/repos/{github_repository}/actions/runs/{github_run}"
    )
    resp.raise_for_status()
    return resp.json()


def get_past_runs(sess: requests.Session, current_run: Any) -> Iterable[Any]:
    """
    Get all successful workflow runs before our current one.
    """
    params = {
        "status": "success",
        "created": f"<={current_run['created_at']}",
        "per_page": 100,
    }
    url = f"{current_run['workflow_url']}/runs"

    while url:
        resp = sess.get(url, params=params)
        resp.raise_for_status()

        for run in resp.json()["workflow_runs"]:
            # First past successful run that isn't our current run.
            if run["id"] == current_run["id"]:
                continue

            yield run

        next_url = resp.links.get("next", {}).get("url")
        if not next_url:
            break

        url = next_url
        params = None


def get_last_publish_sha() -> str:
    github_repository = os.environ["GITHUB_REPOSITORY"]
    github_run = os.environ["GITHUB_RUN_ID"]
    github_token = os.environ["GITHUB_TOKEN"]

    session = requests.Session()
    session.headers["Authorization"] = f"Bearer {github_token}"
    session.headers["Accept"] = "application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"

    most_recent = get_most_recent_workflow(session, github_repository, github_run)
    last_sha = most_recent["head_commit"]["id"]
    print(f"Last successful publish job was {most_recent['id']}: {last_sha}")

    return last_sha


def main():
    last_sha = get_last_publish_sha()

    github_output = os.environ.get("GITHUB_OUTPUT")
    if github_output:
        # Write to the action step output so later steps can reuse it.
        with open(github_output, "a") as f:
            f.write(f"last_publish_sha={last_sha}\n")


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"Failed to compute last publish SHA: {e}", file=sys.stderr)
        exit(1)
