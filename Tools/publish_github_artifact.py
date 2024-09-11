#!/usr/bin/env python3

import requests
import os
import subprocess

GITHUB_TOKEN = os.environ["GITHUB_TOKEN"]
PUBLISH_TOKEN = os.environ["PUBLISH_TOKEN"]
ARTIFACT_ID = os.environ["ARTIFACT_ID"]
GITHUB_REPOSITORY = os.environ["GITHUB_REPOSITORY"]
VERSION = os.environ['GITHUB_SHA']

#
# CONFIGURATION PARAMETERS
# Forks should change these to publish to their own infrastructure.
#
ROBUST_CDN_URL = "https://wizards.cdn.spacestation14.com/"
FORK_ID = "wizards"

def main():
    print("Fetching artifact URL from API...")
    artifact_url = get_artifact_url()
    print(f"Artifact URL is {artifact_url}, publishing to Robust.Cdn")

    data = {
        "version": VERSION,
        "engineVersion": get_engine_version(),
        "archive": artifact_url
    }
    headers = {
        "Authorization": f"Bearer {PUBLISH_TOKEN}",
        "Content-Type": "application/json"
    }
    resp = requests.post(f"{ROBUST_CDN_URL}fork/{FORK_ID}/publish", json=data, headers=headers)
    resp.raise_for_status()
    print("Publish succeeded!")

def get_artifact_url() -> str:
    headers = {
        "Authorization": f"Bearer {GITHUB_TOKEN}",
        "X-GitHub-Api-Version": "2022-11-28"
    }
    resp = requests.get(f"https://api.github.com/repos/{GITHUB_REPOSITORY}/actions/artifacts/{ARTIFACT_ID}/zip", allow_redirects=False, headers=headers)
    resp.raise_for_status()

    return resp.headers["Location"]

def get_engine_version() -> str:
    proc = subprocess.run(["git", "describe","--tags", "--abbrev=0"], stdout=subprocess.PIPE, cwd="RobustToolbox", check=True, encoding="UTF-8")
    tag = proc.stdout.strip()
    assert tag.startswith("v")
    return tag[1:] # Cut off v prefix.


if __name__ == '__main__':
    main()
