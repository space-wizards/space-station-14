#!/usr/bin/env python3

# Generates build info and injects it into the server zip files.

import hashlib
import json
import os
import subprocess
from zipfile import ZipFile, ZIP_DEFLATED

FILE = "SS14.Client.zip"

SERVER_FILES = [
    "SS14.Server_Linux_x64.zip",
    "SS14.Server_Linux_ARM64.zip",
    "SS14.Server_Windows_x64.zip",
    "SS14.Server_macOS_x64.zip"
]

FORK_ID = "wizards"


def main() -> None:
    manifest = generate_manifest("release")

    for server in SERVER_FILES:
        inject_manifest(os.path.join("release", server), manifest)


def inject_manifest(zip_path: str, manifest: str) -> None:
    with ZipFile(zip_path, "a", compression=ZIP_DEFLATED) as z:
        z.writestr("build.json", manifest)


def generate_manifest(dir: str) -> str:
    # Env variables set by Jenkins.

    version = os.environ["BUILD_NUMBER"]
    download = f"{os.environ['BUILD_URL']}artifact/release/{FILE}"
    hash = sha256_file(os.path.join(dir, FILE))
    engine_version = get_engine_version()

    return json.dumps({
        "download": download,
        "hash": hash,
        "version": version,
        "fork_id": FORK_ID,
        "engine_version": engine_version
    })


def get_engine_version() -> str:
    proc = subprocess.run(["git", "describe", "--exact-match", "--tags", "--abbrev=0"], stdout=subprocess.PIPE, cwd="RobustToolbox", check=True, encoding="UTF-8")
    tag = proc.stdout.strip()
    assert tag.startswith("v")
    return tag[1:] # Cut off v prefix.


def sha256_file(path: str) -> str:
    with open(path, "rb") as f:
        h = hashlib.sha256()
        for b in iter(lambda: f.read(4096), b""):
            h.update(b)

        return h.hexdigest()


if __name__ == '__main__':
    main()
