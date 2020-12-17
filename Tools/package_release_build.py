#!/usr/bin/env python3
# Packages a full release build that can be unzipped and you'll have your SS14 client or server.

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

PLATFORM_WINDOWS = "windows"
PLATFORM_LINUX = "linux"
PLATFORM_LINUX_ARM64 = "linux-arm64"
PLATFORM_MACOS = "mac"

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
    "emotes.xml",
    "Groups"
}
SERVER_IGNORED_RESOURCES = {
    "Textures",
    "Fonts",
    "Audio",
    "Scenes",
    "Nano",
    "Shaders",
}

LAUNCHER_RESOURCES = {
    "Nano",
    "Fonts",
}

# Assembly names to copy from content.
# PDBs are included if available, .dll/.pdb appended automatically.
SERVER_CONTENT_ASSEMBLIES = [
    "Content.Server.Database",
    "Content.Server",
    "Content.Shared"
]

CLIENT_CONTENT_ASSEMBLIES = [
    "Content.Client",
    "Content.Shared"
]

# Extra assemblies to copy on the server, with a startswith
SERVER_EXTRA_ASSEMBLIES = [
    "Npgsql.",
    "Microsoft",
]

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Packages the SS14 content repo for release on all platforms.")
    parser.add_argument("--platform",
                        "-p",
                        action="store",
                        choices=[PLATFORM_WINDOWS, PLATFORM_MACOS, PLATFORM_LINUX, PLATFORM_LINUX_ARM64],
                        nargs="*",
                        help="Which platform to build for. If not provided, all platforms will be built")

    parser.add_argument("--skip-build",
                        action="store_true",
                        help=argparse.SUPPRESS)

    args = parser.parse_args()
    platforms = args.platform
    skip_build = args.skip_build

    if not platforms:
        platforms = [PLATFORM_WINDOWS, PLATFORM_MACOS, PLATFORM_LINUX]

    if os.path.exists("release"):
        print(Fore.BLUE + Style.DIM +
              "Cleaning old release packages (release/)..." + Style.RESET_ALL)
        shutil.rmtree("release")

    os.mkdir("release")

    if PLATFORM_WINDOWS in platforms:
        if not skip_build:
            wipe_bin()
        build_windows(skip_build)

    if PLATFORM_LINUX in platforms:
        if not skip_build:
            wipe_bin()
        build_linux(skip_build)

    if PLATFORM_LINUX_ARM64 in platforms:
        if not skip_build:
            wipe_bin()
        build_linux_arm64(skip_build)

    if PLATFORM_MACOS in platforms:
        if not skip_build:
            wipe_bin()
        build_macos(skip_build)


def wipe_bin():
    print(Fore.BLUE + Style.DIM +
          "Clearing old build artifacts (if any)..." + Style.RESET_ALL)
    if os.path.exists(p("RobustToolbox", "bin")):
        shutil.rmtree(p("RobustToolbox", "bin"))

    if os.path.exists("bin"):
        shutil.rmtree("bin")


def build_windows(skip_build: bool) -> None:
    # Run a full build.
    print(Fore.GREEN + "Building project for Windows x64..." + Style.RESET_ALL)

    if not skip_build:
        subprocess.run([
            "dotnet",
            "build",
            "SpaceStation14.sln",
            "-c", "Release",
            "--nologo",
            "/v:m",
            "/p:TargetOS=Windows",
            "/t:Rebuild",
            "/p:FullRelease=True"
        ], check=True)

        publish_client_server("win-x64", "Windows")

    print(Fore.GREEN + "Packaging Windows x64 client..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "SS14.Client_Windows_x64.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_dir_into_zip(p("RobustToolbox", "bin", "Client", "win-x64", "publish"), "", client_zip)
    copy_resources("Resources", client_zip, server=False)
    copy_content_assemblies(p("Resources", "Assemblies"), client_zip, server=False)
    # Cool we're done.
    client_zip.close()

    print(Fore.GREEN + "Packaging Windows x64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_Windows_x64.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server", "win-x64", "publish"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    copy_content_assemblies(p("Resources", "Assemblies"), server_zip, server=True)
    server_zip.close()

def build_macos(skip_build: bool) -> None:
    print(Fore.GREEN + "Building project for macOS x64..." + Style.RESET_ALL)

    if not skip_build:
        subprocess.run([
            "dotnet",
            "build",
            "SpaceStation14.sln",
            "-c", "Release",
            "--nologo",
            "/v:m",
            "/p:TargetOS=MacOS",
            "/t:Rebuild",
            "/p:FullRelease=True"
        ], check=True)

        publish_client_server("osx-x64", "MacOS")

    print(Fore.GREEN + "Packaging macOS x64 client..." + Style.RESET_ALL)
    # Client has to go in an app bundle.
    client_zip = zipfile.ZipFile(p("release", "SS14.Client_macOS_x64.zip"), "a",
                                 compression=zipfile.ZIP_DEFLATED)

    contents = p("Space Station 14.app", "Contents", "Resources")
    copy_dir_into_zip(p("BuildFiles", "Mac", "Space Station 14.app"), "Space Station 14.app", client_zip)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Client", "osx-x64", "publish"), contents, client_zip)
    copy_resources(p(contents, "Resources"), client_zip, server=False)
    copy_content_assemblies(p(contents, "Resources", "Assemblies"), client_zip, server=False)
    client_zip.close()

    print(Fore.GREEN + "Packaging macOS x64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_macOS_x64.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server", "osx-x64", "publish"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    copy_content_assemblies(p("Resources", "Assemblies"), server_zip, server=True)
    server_zip.close()


def build_linux(skip_build: bool) -> None:
    # Run a full build.
    print(Fore.GREEN + "Building project for Linux x64..." + Style.RESET_ALL)

    if not skip_build:
        subprocess.run([
            "dotnet",
            "build",
            "SpaceStation14.sln",
            "-c", "Release",
            "--nologo",
            "/v:m",
            "/p:TargetOS=Linux",
            "/t:Rebuild",
            "/p:FullRelease=True"
        ], check=True)

        publish_client_server("linux-x64", "Linux")

    print(Fore.GREEN + "Packaging Linux x64 client..." + Style.RESET_ALL)

    client_zip = zipfile.ZipFile(
        p("release", "SS14.Client_Linux_x64.zip"), "w",
        compression=zipfile.ZIP_DEFLATED)

    copy_dir_into_zip(p("RobustToolbox", "bin", "Client", "linux-x64", "publish"), "", client_zip)
    copy_resources("Resources", client_zip, server=False)
    copy_content_assemblies(p("Resources", "Assemblies"), client_zip, server=False)
    # Cool we're done.
    client_zip.close()

    print(Fore.GREEN + "Packaging Linux x64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_Linux_x64.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server", "linux-x64", "publish"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    copy_content_assemblies(p("Resources", "Assemblies"), server_zip, server=True)
    server_zip.close()


def build_linux_arm64(skip_build: bool) -> None:
    # Run a full build.
    print(Fore.GREEN + "Building project for Linux ARM64 (SERVER ONLY)..." + Style.RESET_ALL)

    if not skip_build:
        subprocess.run([
            "dotnet",
            "build",
            "SpaceStation14.sln",
            "-c", "Release",
            "--nologo",
            "/v:m",
            "/p:TargetOS=Linux",
            "/t:Rebuild",
            "/p:FullRelease=True"
        ], check=True)

        publish_client_server("linux-arm64", "Linux", True)

    print(Fore.GREEN + "Packaging Linux ARM64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_Linux_ARM64.zip"), "w",
                                 compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("RobustToolbox", "bin", "Server", "linux-arm64", "publish"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    copy_content_assemblies(p("Resources", "Assemblies"), server_zip, server=True)
    server_zip.close()


def publish_client_server(runtime: str, target_os: str, actually_only_server: bool = False) -> None:
    # Runs dotnet publish on client and server.
    base = [
        "dotnet", "publish",
        "--runtime", runtime,
        "--no-self-contained",
        "-c", "Release",
        f"/p:TargetOS={target_os}",
        "/p:FullRelease=True"
    ]

    if not actually_only_server:
        subprocess.run(base + ["RobustToolbox/Robust.Client/Robust.Client.csproj"], check=True)

    subprocess.run(base + ["RobustToolbox/Robust.Server/Robust.Server.csproj"], check=True)


def copy_resources(target, zipf, server):
    # Content repo goes FIRST so that it won't override engine files as that's forbidden.
    ignore_set = SHARED_IGNORED_RESOURCES
    if server:
        ignore_set = ignore_set.union(SERVER_IGNORED_RESOURCES)
    else:
        ignore_set = ignore_set.union(CLIENT_IGNORED_RESOURCES)

    do_resource_copy(target, "Resources", zipf, ignore_set)
    do_resource_copy(target, p("RobustToolbox", "Resources"), zipf, ignore_set)


def copy_launcher_resources(target, zipf):
    # Copy all engine resources, since those are stripped down enough now.
    do_resource_copy(target, p("RobustToolbox", "Resources"), zipf, SHARED_IGNORED_RESOURCES)
    for folder in LAUNCHER_RESOURCES:
        copy_dir_into_zip(p("Resources", folder), p(target, folder), zipf)


def do_resource_copy(target, source, zipf, ignore_set):
    for filename in os.listdir(source):
        if filename in ignore_set:
            continue

        path = p(source, filename)
        target_path = p(target, filename)
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


def copy_content_assemblies(target, zipf, server):
    files = []
    if server:
        source_dir = p("bin", "Content.Server")
        base_assemblies = SERVER_CONTENT_ASSEMBLIES

        # Additional assemblies that need to be copied such as EFCore.
        for filename in os.listdir(source_dir):
            for extra_assembly_start in SERVER_EXTRA_ASSEMBLIES:
                if filename.startswith(extra_assembly_start):
                    files.append(filename)
                    break

    else:
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
        zipf.write(p(source_dir, x), p(target, x))


def copy_dir_or_file(src: str, dst: str):
    """
    Just something from src to dst. If src is a dir it gets copied recursively.
    """

    if os.path.isfile(src):
        shutil.copy2(src, dst)

    elif os.path.isdir(src):
        shutil.copytree(src, dst)

    else:
        raise IOError("{} is neither file nor directory. Can't copy.".format(src))


if __name__ == '__main__':
    main()
