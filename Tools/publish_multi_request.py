#!/usr/bin/env python3

import argparse
import requests
import os
import subprocess
from typing import Iterable

DRY_RUN = os.environ.get("DRY_RUN", "").lower() in ("1", "true", "yes")
VERSION = os.environ["GITHUB_SHA"]

RELEASE_DIR = "release"

#
# CONFIGURATION PARAMETERS
# Forks should change these to publish to their own infrastructure.
#
ROBUST_CDN_URL = "https://wizards.cdn.spacestation14.com/"
FORK_ID = "wizards"

def get_publish_token() -> str:
    try:
        return os.environ["PUBLISH_TOKEN"]
    except KeyError:
        raise RuntimeError("PUBLISH_TOKEN is not set")


def simulate_publish(fork_id: str, version: str) -> None:
    print(f"[DRY RUN] Would publish version {version} to Robust.CDN fork '{fork_id}'")
    print(f"[DRY RUN]   engine version: {get_engine_version()}")
    files = list(get_files_to_publish())
    print(f"[DRY RUN]   files to upload ({len(files)}):")
    for file in files:
        print(f"[DRY RUN]     - {file}")
    print("[DRY RUN] Skipping all network calls to Robust.CDN.")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--fork-id", default=FORK_ID)

    args = parser.parse_args()
    fork_id = args.fork_id


    if DRY_RUN:
        simulate_publish(fork_id, VERSION)
        return

    session = requests.Session()
    session.headers = {
        "Authorization": f"Bearer {get_publish_token()}",
    }

    print(f"Starting publish on Robust.Cdn for version {VERSION}")

    data = {
        "version": VERSION,
        "engineVersion": get_engine_version(),
    }
    headers = {
        "Content-Type": "application/json"
    }
    resp = session.post(f"{ROBUST_CDN_URL}fork/{fork_id}/publish/start", json=data, headers=headers)
    resp.raise_for_status()
    print("Publish successfully started, adding files...")

    for file in get_files_to_publish():
        print(f"Publishing {file}")
        with open(file, "rb") as f:
            headers = {
                "Content-Type": "application/octet-stream",
                "Robust-Cdn-Publish-File": os.path.basename(file),
                "Robust-Cdn-Publish-Version": VERSION
            }
            resp = session.post(f"{ROBUST_CDN_URL}fork/{fork_id}/publish/file", data=f, headers=headers)

        resp.raise_for_status()

    print("Successfully pushed files, finishing publish...")

    data = {
        "version": VERSION
    }
    headers = {
        "Content-Type": "application/json"
    }
    resp = session.post(f"{ROBUST_CDN_URL}fork/{fork_id}/publish/finish", json=data, headers=headers)
    resp.raise_for_status()

    print("SUCCESS!")


def get_files_to_publish() -> Iterable[str]:
    for file in os.listdir(RELEASE_DIR):
        yield os.path.join(RELEASE_DIR, file)


def get_engine_version() -> str:
    proc = subprocess.run(["git", "describe","--tags", "--abbrev=0"], stdout=subprocess.PIPE, cwd="RobustToolbox", check=True, encoding="UTF-8")
    tag = proc.stdout.strip()
    assert tag.startswith("v")
    return tag[1:] # Cut off v prefix.


if __name__ == '__main__':
    main()
