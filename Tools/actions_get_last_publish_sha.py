#!/usr/bin/env python3

"""
Computes the SHA of the last successful GitHub Actions workflow run (on any branch).
Stores found SHA into last_publish_sha output variable for later steps to use.

remark: if previous launch was done on branch that was deleted and commits from there were
not added to tree - this WILL FAIL.
"""

import os
import sys

import actions_changelog_github


def get_last_publish_sha() -> str:
    github_repository = os.environ.get("GITHUB_REPOSITORY")
    github_run = os.environ.get("GITHUB_RUN_ID")
    github_token = os.environ.get("GITHUB_TOKEN")

    if not github_repository:
        raise RuntimeError("GITHUB_REPOSITORY is not set")
    if not github_run:
        raise RuntimeError("GITHUB_RUN_ID is not set")
    if not github_token:
        raise RuntimeError("GITHUB_TOKEN is not set")

    session = actions_changelog_github.make_github_session(github_token)
    return actions_changelog_github.get_last_publish_sha(
        session, github_repository, github_run
    )


def main():
    github_output = os.environ.get("GITHUB_OUTPUT")

    # Don't overwrite if the value was already set by an earlier step/run.
    if os.environ.get("LAST_PUBLISH_SHA"):
        print("LAST_PUBLISH_SHA is already set, skipping")
        return

    if github_output and os.path.exists(github_output):
        with open(github_output, "r", encoding="utf-8") as f:
            lines = f.readlines()

        remaining_lines = []
        already_set = False
        for line in lines:
            stripped = line.strip()
            if stripped.startswith("last_publish_sha="):
                existing_sha = stripped[len("last_publish_sha="):].strip()
                if existing_sha:
                    already_set = True
                    remaining_lines.append(line)
                # else: empty stale marker, drop it so it can't shadow the real value.
            else:
                remaining_lines.append(line)

        if already_set:
            print("last_publish_sha is already set in GITHUB_OUTPUT, skipping")
            return

        if len(remaining_lines) != len(lines):
            with open(github_output, "w", encoding="utf-8") as f:
                f.writelines(remaining_lines)

    last_sha = get_last_publish_sha()

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
