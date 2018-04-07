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

p = os.path.join

SHARED_IGNORED_RESOURCES = {
    "ss13model.7z",
    "ResourcePack.zip",
    "buildResourcePack.py",
    "CONTENT_GOES_HERE"
}
CLIENT_IGNORED_RESOURCES = {
    "Maps",
    "emotes.xml"
}
SERVER_IGNORED_RESOURCES = {
    "Textures",
    "Fonts"
}

GODOT = None

def main():
    global GODOT
    parser = argparse.ArgumentParser(
        description="Packages the SS14 content repo for release on all platforms.")
    parser.add_argument("--platform",
                        "-p",
                        action="store",
                        choices=["windows", "mac", "linux"],
                        nargs="*",
                        help="Which platform to build for. If not provided, all platforms will be built")

    parser.add_argument("--godot",
                        action="store",
                        help="Path to the Godot executable used for exporting.")

    parser.add_argument("--windows-godot-build",
                        action="store")

    args = parser.parse_args()
    platforms = args.platform
    GODOT = args.godot
    if not GODOT:
        print("No Godot executable passed.")
        exit(1)

    if not platforms:
        platforms = ["windows", "mac", "linux"]

    if os.path.exists("release"):
        print(Fore.BLUE+Style.DIM +
              "Cleaning old release packages (release/)..." + Style.RESET_ALL)
        shutil.rmtree("release")

    os.mkdir("release")

    if "windows" in platforms:
        wipe_bin()
        if not args.windows_godot_build:
            print("No --window-godot-build passed")
            exit(1)
        build_windows(args.windows_godot_build)

    if "linux" in platforms:
        wipe_bin()
        build_linux()

    if "mac" in platforms:
        wipe_bin()
        build_macos()


def wipe_bin():
    print(Fore.BLUE + Style.DIM +
          "Clearing old build artifacts (if any)..." + Style.RESET_ALL)
    if os.path.exists(p("engine", "bin")):
        shutil.rmtree(p("engine", "bin"))

    if os.path.exists("bin"):
        shutil.rmtree("bin")


def build_windows(godot_build):
    # Run a full build.
    print(Fore.GREEN + "Building project for Windows x64..." + Style.RESET_ALL)
    subprocess.run(["msbuild",
                    "SpaceStation14Content.sln",
                    "/m",
                    "/p:Configuration=Release",
                    "/p:Platform=x64",
                    "/nologo",
                    "/v:m",
                    "/p:TargetOS=Windows",
                    "/t:Rebuild"
                   ], check=True)

    print(Fore.GREEN + "Packaging Windows x64 client..." + Style.RESET_ALL)

    os.makedirs("bin/win_export", exist_ok=True)
    subprocess.run([GODOT,
                    "--verbose",
                    "--export-debug",
                    "win",
                    "../../bin/win_export/SS14.Client.exe"],
                   cwd="engine/SS14.Client.Godot")

    client_zip = zipfile.ZipFile(p("release", "SS14.Client_Windows_x64.zip"), "w", compression=zipfile.ZIP_DEFLATED)
    client_zip.writestr("spess.bat", "cd godot\ncall SS14.Client.exe --path SS14.Client.Godot")
    client_zip.write(p("bin", "win_export"), "godot")
    client_zip.write(p("bin", "win_export", "SS14.Client.pck"), p("godot", "SS14.Client.pck"))
    copy_dir_into_zip(godot_build, "godot", client_zip)
    copy_dir_into_zip(p("engine", "bin", "Client"), p("bin", "Client"), client_zip)
    copy_resources(p("bin", "Client", "Resources"), client_zip, server=False)
    client_zip.close()

    print(Fore.GREEN + "Packaging Windows x64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_Windows_x64.zip"), "w", compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("engine", "bin", "Server"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    server_zip.close()


def build_linux():
    print(Fore.GREEN + "Building project for Linux x64..." + Style.RESET_ALL)
    subprocess.run(["msbuild",
                    "SpaceStation14Content.sln",
                    "/m",
                    "/p:Configuration=Release",
                    "/p:Platform=x64",
                    "/nologo",
                    "/v:m",
                    "/p:TargetOS=Linux",
                    "/t:Rebuild"
                    ], check=True)

    # NOTE: Temporarily disabled because I can't test it.
    # Package client.
    #print(Fore.GREEN + "Packaging Linux x64 client..." + Style.RESET_ALL)
    # package_zip(p("bin", "Client"), p(
    #    "release", "SS14.Client_linux_x64.zip"))

    print(Fore.GREEN + "Packaging Linux x64 server..." + Style.RESET_ALL)
    server_zip = zipfile.ZipFile(p("release", "SS14.Server_Linux_x64.zip"), "w", compression=zipfile.ZIP_DEFLATED)
    copy_dir_into_zip(p("engine", "bin", "Server"), "", server_zip)
    copy_resources(p("Resources"), server_zip, server=True)
    server_zip.close()



def build_macos():
    print(Fore.GREEN + "Building project for MacOS x64..." + Style.RESET_ALL)
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

    print(Fore.GREEN + "Packaging MacOS x64 client..." + Style.RESET_ALL)
    # Client has to go in an app bundle.
    subprocess.run(GODOT,
                   "--verbose",
                   "--export-debug",
                   "mac",
                   "../../release/mac_export.zip",
                   cwd="engine/SS14.Client.Godot",
                   check=True)

    _copytree(p("engine", "bin", "Client"),
              p(bundle, "Contents", "MacOS", "bin", "Client"))

    copy_resources(p(bundle, "Contents",
                                "MacOS", "bin", "Client", "Resources"), server=False)

    os.makedirs(p(bundle, "Contents", "MacOS",
                             "SS14.Client.Godot"), exist_ok=True)

    _copytree(p("engine", "SS14.Client.Godot"),
              p(bundle, "Contents", "MacOS", "SS14.Client.Godot"))

    package_zip(p("bin", "mac_app"),
                p("release", "SS14.Client_MacOS.zip"))

    print(Fore.GREEN + "Packaging MacOS x64 server..." + Style.RESET_ALL)
    copy_resources(p("engine", "bin",
                                "Server", "Resources"), server=True)

    package_zip(p("engine", "bin", "Server"),
                p("release", "SS14.Server_MacOS.zip"))


def copy_resources(target, zipf, server):
    # Content repo goes FIRST so that it won't override engine files as that's forbidden.
    do_resource_copy(target, "Resources", zipf, server)
    do_resource_copy(target, p("engine", "Resources"), zipf, server)


def do_resource_copy(target, source, zipf, server):
    for filename in os.listdir(source):
        if filename in SHARED_IGNORED_RESOURCES \
                or filename in (SERVER_IGNORED_RESOURCES if server else CLIENT_IGNORED_RESOURCES):
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


if __name__ == '__main__':
    main()
