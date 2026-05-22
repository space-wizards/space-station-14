#!/usr/bin/env python3
"""vgstation14 divergence check.

Fails when a PR modifies or deletes upstream files outside the additive `_VG/`
zones without also updating docs/vg/divergence-registry.md.

Usage: python3 Tools/_VG/check_divergence.py <name-status-file>
where <name-status-file> is the output of `git diff --name-status <base>...<head>`.
"""
import sys

REGISTRY = "docs/vg/divergence-registry.md"

# A path is "additive" (exempt from registry) if it sits in one of these zones.
ADDITIVE_PREFIXES = ("docs/vg/", "docs/superpowers/")


def is_additive(path):
    if path.startswith("_VG/"):
        return True
    if path.startswith(ADDITIVE_PREFIXES):
        return True
    return "/_VG/" in path


def parse(lines):
    """Yield (status, path) pairs from `git diff --name-status` output."""
    for line in lines:
        line = line.rstrip("\r\n")
        if not line:
            continue
        parts = line.split("\t")
        status = parts[0]
        # Renames/copies look like `R100<TAB>old<TAB>new`; use the new path.
        path = parts[-1]
        yield status[0], path


def main(argv):
    if len(argv) != 2:
        print("usage: check_divergence.py <name-status-file>", file=sys.stderr)
        return 2
    try:
        with open(argv[1], encoding="utf-8") as fh:
            changes = list(parse(fh))
    except OSError as exc:
        print(f"error: cannot read '{argv[1]}': {exc}", file=sys.stderr)
        return 2

    changed_paths = {path for _, path in changes}
    registry_updated = REGISTRY in changed_paths

    # Conflict surface = modified, deleted, or renamed upstream files outside
    # the _VG zones. For renames `parse` yields the new path, so a rename into
    # a _VG/ folder is correctly treated as additive.
    divergent = sorted(
        path for status, path in changes
        if status in ("M", "D", "R")
        and path != REGISTRY
        and not is_additive(path)
    )

    if divergent and not registry_updated:
        print("ERROR: this PR changes upstream files outside the _VG/ zones")
        print(f"but does not update {REGISTRY}.")
        print("Either move the change into a _VG/ folder, or record the")
        print("upstream edit in the divergence registry.")
        print("")
        print("Unregistered upstream changes:")
        for path in divergent:
            print(f"  - {path}")
        return 1

    if divergent:
        print(f"OK: {len(divergent)} registered upstream change(s); "
              f"{REGISTRY} updated.")
    else:
        print("OK: all changes are additive (_VG/ zones only).")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
