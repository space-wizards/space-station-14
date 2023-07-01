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

def _print_pycmd_for_platform(script, args):
    """
    Prints a python command from the space-station-14 directory.
    """
    if IS_WINDOWS:
        print(Fore.GREEN + "E:\\ilo\\space-station-14\\> py -3 " + script + " " + (" ").join(args) + Style.RESET_ALL)
    else:
        print(Fore.GREEN + "jan@ilo:~/space-station-14$ python3 " + script + " " + (" ").join(args) + Style.RESET_ALL)

def _are_we_in_correct_directory():
    """
    Returns if we're in the right directory.
    """
    try:
        os.stat("Content.Shared/Content.Shared.csproj")
    except FileNotFoundError:
        return False
    return True

def init(tool):
    """
    Sets the current directory to be the space-station-14 directory, and does some diagnostics.
    """
    # firstly check that we actually need to do this
    # because the structure we're in could be "weird", so if we don't need to do this we shouldn't!
    if not _are_we_in_correct_directory():
        # *then* attempt recovery
        baseline = os.path.realpath(sys.argv[0])
        # if we're being called from the terminal for SOME reason:
        #  == Tools
        # otherwise
        #  == something.py
        # let's just try to be sure...
        if os.path.basename(baseline) != "Tools":
            baseline = os.path.dirname(baseline)
        # ok, now we have the path of Tools, it's whatever is immediately outside that
        baseline = os.path.dirname(baseline)
        os.chdir(baseline)
        # print(Fore.YELLOW + "Automatically changed directory: " + baseline + Style.RESET_ALL)
    if not _are_we_in_correct_directory():
        print(Fore.RED + "For an unknown reason, Content.Shared/Content.Shared.csproj is missing. Continuing regardless." + Style.RESET_ALL)
    try:
        os.stat("Content.Shared/Content.Shared.csproj")
    except FileNotFoundError:
        print(Fore.RED + "For an unknown reason, Content.Shared/Content.Shared.csproj is missing. Continuing regardless." + Style.RESET_ALL)
    try:
        os.stat(".git")
    except FileNotFoundError:
        print(Fore.YELLOW + tool + " was executed with a non-Git source tree.")
        print("Do not use the 'Download ZIP' button on GitHub.")
        print("Instead, use a Git client. In Git Bash, the command would be:" + Style.RESET_ALL)
        print(Fore.GREEN + "jan@ilo:~$ git clone https://github.com/space-wizards/space-station-14" + Style.RESET_ALL)
        print(Fore.YELLOW + "You may add '--depth=1 --recursive' if you wish to reduce clone time/disk space, but beware that this still carries caveats.")
        print("Be sure to also run the RUN_THIS.py script:" + Style.RESET_ALL)
        _print_pycmd_for_platform("RUN_THIS.py", [])
        print(Fore.YELLOW + "Continuing regardless..." + Style.RESET_ALL) # user may know what they're doing, in which case blocking them would not be appreciated
    try:
        os.stat("RobustToolbox/Robust.Shared/Robust.Shared.csproj")
    except FileNotFoundError:
        print(Fore.YELLOW + tool + " did not find Robust.Shared.csproj, which usually means RUN_THIS.py has not been executed:")
        _print_pycmd_for_platform("RUN_THIS.py", [])
        print(Fore.YELLOW + "Continuing regardless..." + Style.RESET_ALL)

if __name__ == '__main__':
    init("package_lib")
    print(Fore.RED + "package_lib.py is a library and not meant to be executed directly." + Style.RESET_ALL)
    print(Fore.RED + "Instead, execute Tools/package_server_build.py (in most cases) or that and Tools/package_client_build.py (for if you're hosting a server with a separate client build server)." + Style.RESET_ALL)
    exit(1)

