# Git Integration Hooks

This folder contains installable scripts for [Git hooks] and [merge drivers].
Use of these hooks and drivers is optional and they must be installed
explicitly before they take effect.

To install the current set of hooks, or update if new hooks are added, run
`install.bat` (Windows) or `install.sh` (Unix-like) as appropriate.

Hooks expect a Unix-like environment on the backend. Usually this is handled
automatically by GUI tools like TortoiseGit and GitHub for Windows, but
[Git for Windows] is an option if you prefer to use a CLI even on Windows.

## Current Hooks

* **Pre-commit**: Runs [mapmerge2] on changed maps, if any.
* **DMI merger**: Attempts to [fix icon conflicts] when performing a git merge.
  If it succeeds, the file is marked merged. If it fails, it logs what states
  are still in conflict and adds them to the .dmi file, where the desired
  resolution can be chosen.

## Adding New Hooks

New [Git hooks] may be added by creating a file named `<hook-name>.hook` in
this directory. Git determines what hooks are available and what their names
are. The install script copies the `.hook` file into `.git/hooks`, so editing
the `.hook` file will require a reinstall.

New [merge drivers] may be added by adding a shell script named `<ext>.merge`
and updating `.gitattributes` in the root of the repository to include the line
`*.<ext> merge=<ext>`. The install script will set up the merge driver to point
to the `.merge` file directly, and editing it will not require a reinstall.

`tools/hooks/python.sh` may be used as a trampoline to ensure that the correct
version of Python is found.

[Git hooks]: https://git-scm.com/book/en/v2/Customizing-Git-Git-Hooks
[merge drivers]: https://git-scm.com/docs/gitattributes#_performing_a_three_way_merge
[Git for Windows]: https://gitforwindows.org/
[mapmerge2]: ../mapmerge2/README.md
[fix icon conflicts]: ../mapmerge2/merge_driver_dmi.py
