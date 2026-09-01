#!/usr/bin/env python3

"""
Shared GitHub Actions API helpers for the changelog publishing scripts.

Contains the logic for finding the last successful workflow run of the
current workflow and the commit SHA it ran on, plus an authenticated
requests session helper.
"""

import os
import re
from typing import Any, Iterable

import requests

GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")


def make_github_session(github_token: str) -> requests.Session:
    session = requests.Session()
    session.headers["Authorization"] = f"Bearer {github_token}"
    session.headers["Accept"] = "application/vnd.github+json"
    session.headers["X-GitHub-Api-Version"] = "2022-11-28"
    return session


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
    run = resp.json()
    for key in ("id", "created_at", "workflow_url"):
        if key not in run:
            raise RuntimeError(
                f"GitHub API response for current run is missing '{key}'"
            )

    return run


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

        runs = resp.json()
        if "workflow_runs" not in runs:
            raise RuntimeError(
                "GitHub API response for past runs is missing 'workflow_runs'"
            )

        for run in runs["workflow_runs"]:
            # First past successful run that isn't our current run.
            if run["id"] == current_run["id"]:
                continue

            yield run

        next_url = resp.links.get("next", {}).get("url")
        if not next_url:
            break

        url = next_url
        params = None


def get_last_publish_sha(
    sess: requests.Session, github_repository: str, github_run: str
) -> str:
    """
    Get the head commit SHA of the most recent successful workflow run.

    Validates that the found run actually has a head commit id and that the
    value looks like a commit SHA before returning it.
    """
    most_recent = get_most_recent_workflow(sess, github_repository, github_run)

    head_commit = most_recent.get("head_commit")
    last_sha = head_commit.get("id") if head_commit else None
    if not last_sha:
        raise RuntimeError(
            f"Workflow run {most_recent.get('id')} has no head_commit id"
        )

    # Sanity-check that the value looks like a commit SHA before publishing it.
    if not re.fullmatch(r"[0-9a-fA-F]{7,64}", last_sha):
        raise RuntimeError(
            f"Computed last publish SHA does not look like a commit hash: {last_sha!r}"
        )

    print(f"Last successful publish job was {most_recent.get('id')}: {last_sha}")

    return last_sha
