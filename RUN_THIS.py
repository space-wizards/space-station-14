#!/usr/bin/env python3

# Import future so people on py2 still get the clear error that they need to upgrade.
from __future__ import print_function
import sys
import subprocess

IS_WINDOWS = sys.platform in ("win32", "cygwin")

version = sys.version_info
if version.major < 3 or (version.major == 3 and version.minor < 5):
    print("ERROR: You need at least Python 3.5 to build SS14.")
    sys.exit(1)

if IS_WINDOWS:
    subprocess.run(["py", "-3", "git_helper.py"], cwd="BuildChecker")
else:
    subprocess.run(["python3", "git_helper.py"], cwd="BuildChecker")
