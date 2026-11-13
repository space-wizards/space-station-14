#!/usr/bin/env python3

"""
Collects information about the sources the current build was made from and exposes it
to other GitHub Actions step outputs.

Outputs inlcude following items with respective values:
    source_url
    source_commit_id
    source_branch_name
    engine_source_url
    engine_source_commit_id
    engine_source_branch_name

Every value can be overridden with an environment variable of the same name in upper case
(e.g. SOURCE_BRANCH_NAME). Detection failures are non-fatal: the value is left as empty string.
"""

import os
import re
import subprocess
import sys

ENGINE_DIR = "RobustToolbox"
DEFAULT_SERVER_URL = "https://github.com"


def run_git(*args: str, cwd: str = ".") -> list[str]:
    try:
        proc = subprocess.run(
            ["git", *args],
            cwd=cwd,
            check=True,
            capture_output=True,
            text=True,
        )
    except (subprocess.CalledProcessError, FileNotFoundError, OSError):
        return []

    lines = []
    for line in proc.stdout.splitlines():
        line = line.strip()
        if line:
            lines.append(line)

    return lines


def run_git_get_first_line(*args: str, cwd: str = ".") -> str:
    lines = run_git(*args, cwd=cwd)
    return lines[0] if lines else ""


def normalize_remote_url(url: str) -> str:
    url = url.strip()
    if not url:
        return ""

    # scp-like syntax, e.g. git@github.com:space-wizards/RobustToolbox.git
    scp = re.match(r"^[^/@]+@([^/:]+):(.+)$", url)
    if scp and "://" not in url:
        url = f"https://{scp.group(1)}/{scp.group(2)}"

    # Strip credentials. GitHub Actions rewrites remotes to contain access tokens.
    url = re.sub(r"^([a-zA-Z][a-zA-Z0-9+.-]*://)[^/@]*@", r"\1", url)

    if url.endswith(".git"):
        url = url[: -len(".git")]

    return url.rstrip("/")


def get_engine_branch_name() -> str:
    # Prefer the remote branch that contains the pinned commit.
    default_branch = run_git_get_first_line(
        "symbolic-ref", "--short", "refs/remotes/origin/HEAD", cwd=ENGINE_DIR
    ).removeprefix("origin/")

    branches = [
        branch.removeprefix("origin/")
        for branch in run_git(
            "for-each-ref",
            "--contains",
            "HEAD",
            "--format=%(refname:short)",
            "refs/remotes/origin",
            cwd=ENGINE_DIR,
        )
    ]

    if branches:
        if default_branch in branches:
            return default_branch

        return sorted(branches)[0]

    # Not on any remote branch (e.g. a PR pin): fall back to tags.
    exact_tag = run_git_get_first_line("describe", "--tags", "--exact-match", "HEAD", cwd=ENGINE_DIR)
    if exact_tag:
        return exact_tag

    nearest_tag = run_git_get_first_line("describe", "--tags", "--abbrev=0", cwd=ENGINE_DIR)
    if nearest_tag:
        return nearest_tag

    return "unknown"


def collect_source_info() -> dict[str, str]:
    server_url = os.environ.get("GITHUB_SERVER_URL") or DEFAULT_SERVER_URL
    repository = os.environ.get("GITHUB_REPOSITORY")

    if repository:
        source_url = f"{server_url.rstrip('/')}/{repository}"
    else:
        source_url = normalize_remote_url(run_git_get_first_line("remote", "get-url", "origin"))

    reference_name = os.environ.get("GITHUB_REF_NAME")
    branch_name = reference_name or run_git_get_first_line("rev-parse", "--abbrev-ref", "HEAD")
    if branch_name == "HEAD":
        # Detached checkout (pull requests, tags).
        branch_name = reference_name or ""

    info = {
        "source_url": source_url,
        "source_commit_id": run_git_get_first_line("rev-parse", "HEAD"),
        "source_branch_name": branch_name,
        "engine_source_url": normalize_remote_url(
            run_git_get_first_line("remote", "get-url", "origin", cwd=ENGINE_DIR)
        ),
        "engine_source_commit_id": run_git_get_first_line("rev-parse", "HEAD", cwd=ENGINE_DIR),
        "engine_source_branch_name": get_engine_branch_name(),
    }

    # Explicit environment values win over auto-detection.
    for key in info:
        override = os.environ.get(key.upper())
        if override:
            info[key] = override

    # Step outputs are line based, so collapse whitespace instead of passing it through.
    outputs = {}
    for key, value in info.items():
        outputs[key] = " ".join(value.split())

    return outputs


def main():
    info = collect_source_info()

    print("Source info for this build:")
    for key, value in info.items():
        print(f"  {key} = {value or '<unknown>'}")

    # Write the values to the GitHub Actions step output
    github_output = os.environ.get("GITHUB_OUTPUT")
    if github_output:
        with open(github_output, "a", encoding="utf-8") as f:
            for key, value in info.items():
                f.write(f"{key}={value}\n")


if __name__ == "__main__":
    main()
