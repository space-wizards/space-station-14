#!/usr/bin/env python3

import os
import sys

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

IS_WINDOWS = sys.platform in ("win32", "cygwin")

SHARED_IGNORED_RESOURCES = {
    ".gitignore",
    ".directory",
    ".DS_Store"
}

# Diagnose common mistakes

def print_pycmd_for_platform(script, args):
    if IS_WINDOWS:
        print(Fore.GREEN + "E:\\ilo\\space-station-14\\> py -3 " + script + " " + (" ").join(args) + Style.RESET_ALL)
    else:
        print(Fore.GREEN + "jan@ilo:~/space-station-14$ python3 " + script + " " + (" ").join(args) + Style.RESET_ALL)

def diagnose_installation(tool):
    try:
        os.stat("Content.Shared/Content.Shared.csproj")
    except FileNotFoundError:
        print(Fore.RED + "This program cannot be executed from this current directory.")
        print("It must be executed from the space-station-14 directory, for example:" + Style.RESET_ALL)
        print_pycmd_for_platform("Tools/" + tool + ".py", sys.argv[1:])
        exit(1) # this is irrecoverable even for advanced users unless we just plain start *guessing*... let's not
    try:
        os.stat(".git")
    except FileNotFoundError:
        print(Fore.YELLOW + tool + " was executed with a non-Git source tree.")
        print("Do not use the 'Download ZIP' button on GitHub.")
        print("Instead, use a Git client. In Git Bash, the command would be:" + Style.RESET_ALL)
        print(Fore.GREEN + "jan@ilo:~$ git clone https://github.com/space-wizards/space-station-14" + Style.RESET_ALL)
        print(Fore.YELLOW + "You may add '--depth=1 --recursive' if you wish to reduce clone time/disk space, but beware that this still carries caveats.")
        print("Be sure to also run the RUN_THIS.py script:" + Style.RESET_ALL)
        print_pycmd_for_platform("RUN_THIS.py", [])
        print(Fore.YELLOW + "Continuing regardless..." + Style.RESET_ALL) # user may know what they're doing, in which case blocking them would not be appreciated
    try:
        os.stat("RobustToolbox/Robust.Shared/Robust.Shared.csproj")
    except FileNotFoundError:
        print(Fore.YELLOW + tool + " did not find Robust.Shared.csproj, which usually means RUN_THIS.py has not been executed:")
        print_pycmd_for_platform("RUN_THIS.py", [])
        print(Fore.YELLOW + "Continuing regardless..." + Style.RESET_ALL)

if __name__ == '__main__':
    diagnose_installation("package_lib")
    print(Fore.RED + "package_lib.py is a library and not meant to be executed directly." + Style.RESET_ALL)
    print(Fore.RED + "Instead, execute Tools/package_server_build.py (in most cases) or that and Tools/package_client_build.py (for if you're hosting a server with a separate client build server)." + Style.RESET_ALL)
    exit(1)

