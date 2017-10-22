#!/usr/bin/env python3
# Packages a full release build that can be unzipped and you'll have your SS14 client or server.

import os
import shutil
import subprocess
import sys
import zipfile
import argparse

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

def main():
    parser = argparse.ArgumentParser(
        description="Packages the SS14 content repo for release on all platforms.")
    parser.add_argument("--platform",
                        action="store",
                        choices=["windows", "mac", "linux"],
                        nargs="*",
                        help="Which platform to build for. If not provided, all platforms will be built")

    args = parser.parse_args()
    platforms = args.platform

    if not platforms:
        platforms = ["windows", "mac", "linux"]

    if os.path.exists("release"):
        print(Fore.BLUE+Style.DIM + "Cleaning old release packages (release/)..." + Style.RESET_ALL)
        shutil.rmtree("release")

    os.mkdir("release")

    if "windows" in platforms:
        wipe_bin()
        build_windows()

    if "linux" in platforms:
        wipe_bin()
        build_linux()

    if "mac" in platforms:
        wipe_bin()
        build_macos()

def wipe_bin():
    if os.path.exists("bin"):
        print(Fore.BLUE + Style.DIM + "Clearing old build artifacts..." + Style.RESET_ALL)
        shutil.rmtree("bin")

def build_windows():
    # Run a full build.
    print(Fore.GREEN + "Building project for Windows x86..." + Style.RESET_ALL)
    subprocess.run(["msbuild",
                    "SpaceStation14Content.sln",
                    "/m",
                    "/p:Configuration=Release",
                    "/p:Platform=x86",
                    "/nologo",
                    "/v:m",
                    "/p:TargetOS=Windows",
                    "/t:Rebuild"
                   ], check=True)

    # Package client.
    print(Fore.GREEN + "Packaging Windows x86 client..." + Style.RESET_ALL)
    package_zip(os.path.join("bin", "Client"),
                os.path.join("release", "SS14.Client_windows_x86.zip"))

    print(Fore.GREEN + "Packaging Windows x86 server..." + Style.RESET_ALL)
    package_zip(os.path.join("bin", "Server"),
                os.path.join("release", "SS14.Server_windows_x86.zip"))


def build_linux():
    print(Fore.GREEN + "Building project for Linux x86..." + Style.RESET_ALL)
    subprocess.run(["msbuild",
                    "SpaceStation14Content.sln",
                    "/m",
                    "/p:Configuration=Release",
                    "/p:Platform=x86",
                    "/nologo",
                    "/v:m",
                    "/p:TargetOS=Linux",
                    "/t:Rebuild"
                   ], check=True)

    # Package client.
    print(Fore.GREEN + "Packaging Linux x86 client..." + Style.RESET_ALL)
    package_zip(os.path.join("bin", "Client"), os.path.join("release", "SS14.Client_linux_x86.zip"))

    print(Fore.GREEN + "Packaging Linux x86 server..." + Style.RESET_ALL)
    package_zip(os.path.join("bin", "Server"), os.path.join("release", "SS14.Server_linux_x86.zip"))


def build_macos():
    # Haha this is gonna suck.
    print(Fore.GREEN + "Building project for MacOS x86..." + Style.RESET_ALL)
    subprocess.run(["msbuild",
                    "SpaceStation14Content.sln",
                    "/m",
                    "/p:Configuration=Release",
                    "/p:Platform=x64",
                    "/nologo",
                    "/v:m",
                    "/p:TargetOS=MacOS",
                    "/t:Rebuild"
                   ], check=True)

    print(Fore.GREEN + "Packaging MacOS x86 client..." + Style.RESET_ALL)
    # Client has to go in an app bundle.
    bundle = os.path.join("bin", "app", "Space Station 14.app")
    shutil.copytree(os.path.join("BuildFiles", "Mac", "Space Station 14.app"),
                    bundle)

    _copytree(os.path.join("bin", "Client"),
              os.path.join(bundle, "Contents", "MacOS"))

    package_zip(os.path.join("bin", "app"),
                os.path.join("release", "SS14.Client_MacOS_x86.zip"))

    print(Fore.GREEN + "Packaging MacOS x86 server..." + Style.RESET_ALL)
    package_zip(os.path.join("bin", "Server"),
                os.path.join("release", "SS14.Server_MacOS_x86.zip"))

# Hack copied from Stack Overflow to get around the fact that
# shutil.copytree doesn't allow copying into existing directories.
def _copytree(src, dst, symlinks=False, ignore=None):
    for item in os.listdir(src):
        s = os.path.join(src, item)
        d = os.path.join(dst, item)
        if os.path.isdir(s):
            shutil.copytree(s, d, symlinks, ignore)
        else:
            shutil.copy2(s, d)

def package_zip(directory, zipname):
    with zipfile.ZipFile(zipname, "w") as zipf:
        for dirs, _, files in os.walk(directory):
            relpath = os.path.relpath(dirs, directory)
            if relpath != ".":
                # Write directory node except for root level.
                zipf.write(dirs, relpath)

            for filename in files:
                zippath = os.path.join(relpath, filename)
                filepath = os.path.join(dirs, filename)

                message = "{dim}{diskroot}{sep}{zipfile}{dim} -> {ziproot}{sep}{zipfile}".format(
                    sep=os.sep + Style.NORMAL,
                    dim=Style.DIM,
                    diskroot=directory,
                    ziproot=zipname,
                    zipfile=os.path.normpath(zippath))

                print(Fore.CYAN + message + Style.RESET_ALL)
                zipf.write(filepath, zippath)

if __name__ == '__main__':
    main()
