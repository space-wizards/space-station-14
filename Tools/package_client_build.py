#!/usr/bin/env python3

import os
import shutil
import subprocess
import sys
import zipfile
import argparse

from typing import List, Optional

try:
    from colorama import init, Fore, Style
    init()

except ImportError:
    # Just give an empty string for everything, no colored logging.
    class ColorDummy(object):
        def __getattr__(self, name):
            return ""

    Fore = ColorDummy()
    Style = ColorDummy()


p = os.path.join

SHARED_IGNORED_RESOURCES = {
    "ss13model.7z",
    "ResourcePack.zip",
    "buildResourcePack.py",
    "CONTENT_GOES_HERE",
    ".gitignore",
    ".directory",
    ".DS_Store"
}

CLIENT_IGNORED_RESOURCES = {
    "Maps",
    "ConfigPresets",
    "emotes.xml",
    "Groups",
    "engineCommandPerms.yml"
}

CLIENT_CONTENT_ASSEMBLIES = [
    # IF YOU ADD SOMETHING HERE, ADD IT TO MANIFEST.YML TOO.
    "Content.Client",
    "Content.Shared",
    "Content.Shared.Database"
]

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Packages the SS14 client.")

    parser.add_argument("--skip-build",
                        action="store_true",
                        help=argparse.SUPPRESS)

    args = parser.parse_args()
    skip_build = args.skip_build

    if os.path.exists("release"):
        pass
    #    print(Fore.BLUE + Style.DIM +
    #          "Cleaning old release packages (release/)..." + Style.RESET_ALL)
    #    shutil.rmtree("release")
    else:
        os.mkdir("release")

    if not skip_build:
        wipe_bin()

    build(skip_build)


def wipe_bin():
    print(Fore.BLUE + Style.DIM +
          "Clearing old build artifacts (if any)..." + Style.RESET_ALL)

    if os.path.exists("bin"):
        shutil.rmtree("bin")

# Be advised this is called from package_server_build during a Hybrid-ACZ build.
def build(skip_build: bool) -> None:
    # Run a full build.
    print(Fore.GREEN + "Building project..." + Style.RESET_ALL)

    if not skip_build:
        subprocess.run([
            "dotnet",
            "build",
            p("Content.Client", "Content.Client.csproj"),
            "-c", "Release",
            "--nologo",
            "/v:m",
            "/t:Rebuild",
            "/p:FullRelease=True",
            "/m"
        ], check=True)

    print(Fore.GREEN + "Packaging client..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "SS14.Client.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_resources(client_zip)
    copy_content_assemblies("Assemblies", client_zip)
    # Cool we're done.
    client_zip.close()


def copy_resources(zipf):
    ignore_set = SHARED_IGNORED_RESOURCES.union(CLIENT_IGNORED_RESOURCES)

    do_resource_copy("Resources", zipf, ignore_set)


def do_resource_copy(source, zipf, ignore_set):
    for filename in os.listdir(source):
        if filename in ignore_set:
            continue

        path = p(source, filename)
        target_path = filename
        if os.path.isdir(path):
            copy_dir_into_zip(path, target_path, zipf)

        else:
            zipf.write(path, target_path)


def zip_entry_exists(zipf, name):
    try:
        # Trick ZipInfo into sanitizing the name for us so this awful module stops spewing warnings.
        zinfo = zipfile.ZipInfo.from_file("Resources", name)
        zipf.getinfo(zinfo.filename)
    except KeyError:
        return False
    return True


def copy_dir_into_zip(directory, basepath, zipf):
    if basepath and not zip_entry_exists(zipf, basepath):
        zipf.write(directory, basepath)

    for root, _, files in os.walk(directory):
        relpath = os.path.relpath(root, directory)
        if relpath != "." and not zip_entry_exists(zipf, p(basepath, relpath)):
            zipf.write(root, p(basepath, relpath))

        for filename in files:
            zippath = p(basepath, relpath, filename)
            filepath = p(root, filename)

            message = "{dim}{diskroot}{sep}{zipfile}{dim} -> {ziproot}{sep}{zipfile}".format(
                sep=os.sep + Style.NORMAL,
                dim=Style.DIM,
                diskroot=directory,
                ziproot=zipf.filename,
                zipfile=os.path.normpath(zippath))

            print(Fore.CYAN + message + Style.RESET_ALL)
            zipf.write(filepath, zippath)


def copy_content_assemblies(target, zipf):
    files = []

    source_dir = p("bin", "Content.Client")
    base_assemblies = CLIENT_CONTENT_ASSEMBLIES

    # Include content assemblies.
    for asm in base_assemblies:
        files.append(asm + ".dll")
        # If PDB available, include it aswell.
        pdb_path = asm + ".pdb"
        if os.path.exists(p(source_dir, pdb_path)):
            files.append(pdb_path)

    # Write assemblies dir if necessary.
    if not zip_entry_exists(zipf, target):
        zipf.write(".", target)

    for x in files:
        zipf.write(p(source_dir, x), f"{target}/{x}")


if __name__ == '__main__':
    main()
