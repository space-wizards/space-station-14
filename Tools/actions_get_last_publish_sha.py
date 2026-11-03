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
    github_repository, github_run, github_token = (
        actions_changelog_github.get_required_github_env()
    )

    session = actions_changelog_github.make_github_session(github_token)
    return actions_changelog_github.get_last_publish_sha(
        session, github_repository, github_run
    )


def main():
    github_output = os.environ.get("GITHUB_OUTPUT")

    # If the value was already provided via the environment, propagate it to the
    # step output instead of recomputing it.
    last_sha = os.environ.get("LAST_PUBLISH_SHA")
    if last_sha:
        print(f"LAST_PUBLISH_SHA is already set, using it: {last_sha}")
        if github_output:
            with open(github_output, "a") as f:
                f.write(f"last_publish_sha={last_sha}\n")
        return

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
