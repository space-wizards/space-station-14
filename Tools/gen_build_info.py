#!/usr/bin/env python3

# Generates build info and injects it into the server zip files.

import codecs
import hashlib
import io
import json
import os
import subprocess
from zipfile import ZipFile, ZIP_DEFLATED

FILE = "SS14.Client.zip"

SERVER_FILES = [
    "SS14.Server_linux-x64.zip",
    "SS14.Server_linux-arm64.zip",
    "SS14.Server_win-x64.zip",
    "SS14.Server_osx-x64.zip"
]

VERSION = os.environ['GITHUB_SHA']
FORK_ID = "wizards"
BUILD_URL = f"https://cdn.centcomm.spacestation14.com/builds/wizards/builds/{{FORK_VERSION}}/{FILE}"
MANIFEST_URL = f"https://cdn.centcomm.spacestation14.com/cdn/version/{{FORK_VERSION}}/manifest"
MANIFEST_DOWNLOAD_URL = f"https://cdn.centcomm.spacestation14.com/cdn/version/{{FORK_VERSION}}/download"

def main() -> None:
    client_file = os.path.join("release", FILE)
    manifest = generate_build_json(client_file)

    for server in SERVER_FILES:
        inject_manifest(os.path.join("release", server), manifest)


def inject_manifest(zip_path: str, manifest: str) -> None:
    with ZipFile(zip_path, "a", compression=ZIP_DEFLATED) as z:
        z.writestr("build.json", manifest)


def generate_build_json(file: str) -> str:
    # Env variables set by Jenkins.

    hash = sha256_file(file)
    engine_version = get_engine_version()
    manifest_hash = generate_manifest_hash(file)

    return json.dumps({
        "download": BUILD_URL,
        "hash": hash,
        "version": VERSION,
        "fork_id": FORK_ID,
        "engine_version": engine_version,
        "manifest_url": MANIFEST_URL,
        "manifest_download_url": MANIFEST_DOWNLOAD_URL,
        "manifest_hash": manifest_hash
    })

def generate_manifest_hash(file: str) -> str:
    zip = ZipFile(file)
    infos = zip.infolist()
    infos.sort(key=lambda i: i.filename)

    bytesIO = io.BytesIO()
    writer = codecs.getwriter("UTF-8")(bytesIO)
    writer.write("Robust Content Manifest 1\n")

    for info in infos:
        if info.filename[-1] == "/":
            continue

        bytes = zip.read(info)
        hash = hashlib.blake2b(bytes, digest_size=32).hexdigest().upper()
        writer.write(f"{hash} {info.filename}\n")

    manifestHash = hashlib.blake2b(bytesIO.getbuffer(), digest_size=32)

    return manifestHash.hexdigest().upper()

def get_engine_version() -> str:
    proc = subprocess.run(["git", "describe","--tags", "--abbrev=0"], stdout=subprocess.PIPE, cwd="RobustToolbox", check=True, encoding="UTF-8")
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
