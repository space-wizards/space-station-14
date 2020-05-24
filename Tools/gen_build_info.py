#!/usr/bin/env python3

# Generates build info and injects it into the server zip files.

import hashlib
import json
import os
from zipfile import ZipFile, ZIP_DEFLATED

FILES = {
    "linux": "SS14.Client_Linux_x64.zip",
    "windows": "SS14.Client_Windows_x64.zip",
    "macos": "SS14.Client_macOS_x64.zip"
}

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

    jenkins_base = f"{os.environ['BUILD_URL']}artifact/release/"

    version = os.environ["BUILD_NUMBER"]
    downloads = {}
    hashes = {}

    for platform, filename in FILES.items():
        downloads[platform] = f"{jenkins_base}{filename}"
        hashes[platform] = sha256_file(os.path.join(dir, filename))

    return json.dumps({"downloads": downloads, "hashes": hashes, "version": version, "fork_id": FORK_ID})


def sha256_file(path: str) -> str:
    with open(path, "rb") as f:
        h = hashlib.sha256()
        for b in iter(lambda: f.read(4096), b""):
            h.update(b)

        return h.hexdigest()


if __name__ == '__main__':
    main()
