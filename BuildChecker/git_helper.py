#!/usr/bin/env python3
"""
Installs git hooks, updates them, updates submodules, that kind of thing.
"""

import os
import shutil
import subprocess
import sys
import time
from pathlib import Path
from typing import List

SOLUTION_PATH = Path("..") / "SpaceStation14.sln"
# If this doesn't match the saved version we overwrite them all.
CURRENT_HOOKS_VERSION = "4"
QUIET = len(sys.argv) == 2 and sys.argv[1] == "--quiet"


def run_command(command: List[str], capture: bool = False) -> subprocess.CompletedProcess:
    """
    Runs a command with pretty output.
    """
    text = ' '.join(command)
    if not QUIET:
        print("$ {}".format(text))

    sys.stdout.flush()

    if capture:
        completed = subprocess.run(command, stdout=subprocess.PIPE, text=True)
    else:
        completed = subprocess.run(command)

    if completed.returncode != 0:
        print("Error: command exited with code {}!".format(completed.returncode))

    return completed


def update_submodules():
    """
    Updates all submodules.
    """

    if 'GITHUB_ACTIONS' in os.environ:
        return

    if os.path.isfile("DISABLE_SUBMODULE_AUTOUPDATE"):
        return

    if shutil.which("git") is None:
        raise FileNotFoundError("git not found in PATH")

    # If the status doesn't match, force VS to reload the solution.
    # status = run_command(["git", "submodule", "status"], capture=True)
    run_command(["git", "submodule", "update", "--init", "--recursive"])
    # status2 = run_command(["git", "submodule", "status"], capture=True)

    # Something changed.
    # if status.stdout != status2.stdout:
    #     print("Git submodules changed. Reloading solution.")
    #     reset_solution()


def install_hooks():
    """
    Installs the necessary git hooks into .git/hooks.
    """

    # Read version file.
    if os.path.isfile("INSTALLED_HOOKS_VERSION"):
        with open("INSTALLED_HOOKS_VERSION", "r") as f:
            if f.read() == CURRENT_HOOKS_VERSION:
                if not QUIET:
                    print("No hooks change detected.")
                return

    print("Hooks need updating.")

    hooks_target_dir = Path(run_command(["git", "rev-parse", "--git-path", "hooks"], True).stdout.strip())
    hooks_source_dir = Path("hooks")

    # Clear entire tree since we need to kill deleted files too.
    for filename in os.listdir(hooks_target_dir):
        os.remove(hooks_target_dir / filename)

    for filename in os.listdir(hooks_source_dir):
        print("Copying hook {}".format(filename))
        shutil.copy2(hooks_source_dir / filename, hooks_target_dir / filename)

    with open("INSTALLED_HOOKS_VERSION", "w") as f:
        f.write(CURRENT_HOOKS_VERSION)


def reset_solution():
    """
    Force VS to think the solution has been changed to prompt the user to reload it, thus fixing any load errors.
    """

    with SOLUTION_PATH.open("r") as f:
        content = f.read()

    with SOLUTION_PATH.open("w") as f:
        f.write(content)

def check_for_zip_download():
    # Check if .git exists,
    if run_command(["git", "rev-parse"]).returncode != 0:
        print("It appears that you downloaded this repository directly from GitHub. (Using the .zip download option) \n"
              "When downloading straight from GitHub, it leaves out important information that git needs to function. "
              "Such as information to download the engine or even the ability to even be able to create contributions. \n"
              "Please read and follow https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html \n"
              "If you just want a Sandbox Server, you are following the wrong guide! You can download a premade server following the instructions here:"
              "https://docs.spacestation14.com/en/general-development/setup/server-hosting-tutorial.html \n"
              "Closing automatically in 30 seconds.")
        time.sleep(30)
        exit(1)

if __name__ == '__main__':
    check_for_zip_download()
    install_hooks()
    update_submodules()
