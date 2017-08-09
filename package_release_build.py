#!/usr/bin/env python3
# Packages a full release build that can be unzipped and you'll have your SS14 client or server.

import os
import shutil
import subprocess
import zipfile

try:
    from colorama import init, Fore, Back, Style
    init()

except ImportError:
    # Just give an empty string for everything, no colored logging.
    class ColorDummy(object):
        def __getattr__(self, name):
            return ""

    Fore = ColorDummy()
    Style = ColorDummy()
    Back = ColorDummy()

def main():
    # Wipe out old build directory. Cleans potential leftovers to make sure we have a clean build.
    if os.path.exists("bin"):
        print(Fore.BLUE + Style.DIM + "Clearing old build artifacts (bin/)..." + Style.RESET_ALL)
        shutil.rmtree("bin")

    if os.path.exists("release"):
        print(Fore.BLUE + Style.DIM + "Cleaning old release packages (release/)..." + Style.RESET_ALL)
        shutil.rmtree("release")
    os.mkdir("release")

    build_windows()

    if os.path.exists("bin"):
        print(Fore.BLUE + Style.DIM + "Clearing old build artifacts..." + Style.RESET_ALL)
        shutil.rmtree("bin")

    build_linux()

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
                    "/p:TargetOS=Windows_NT",
                    "/t:Rebuild"
                    ], check=True)

    # Package client.
    print(Fore.GREEN + "Packaging Windows x86 client..." + Style.RESET_ALL)
    package_zip(os.path.join("bin", "Client"), os.path.join("release", "SS14.Client_windows_x86.zip"))

    print(Fore.GREEN + "Packaging Windows x86 server..." + Style.RESET_ALL)
    package_zip(os.path.join("bin", "Server"), os.path.join("release", "SS14.Server_windows_x86.zip"))

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

def package_zip(directory, zipname):
    with zipfile.ZipFile(zipname, "w") as f:
        for dir, _, files in os.walk(directory):
            relpath = os.path.relpath(dir, directory)
            if relpath != ".":
                # Write directory node except for root level.
                f.write(dir, relpath)

            for filename in files:
                zippath = os.path.join(relpath, filename)
                filepath = os.path.join(dir, filename)

                print(Fore.CYAN + "{dim}{diskroot}{sep}{zipfile}{dim} -> {ziproot}{sep}{zipfile}"
                    .format(
                        sep = os.sep + Style.NORMAL,
                        dim = Style.DIM,
                        diskroot = directory,
                        ziproot = zipname,
                        zipfile = os.path.normpath(zippath)
                    ) + Style.RESET_ALL)

                f.write(filepath, zippath)

if __name__ == '__main__':
    main()
