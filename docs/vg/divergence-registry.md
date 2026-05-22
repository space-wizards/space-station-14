# vgstation14 — Divergence Registry

Every upstream file vgstation14 has intentionally **modified** or **deleted**.
This is the fork's conflict surface — when an upstream sync conflicts, it
conflicts here. Keep it accurate: the `VG: Divergence check` CI job requires
this file to be updated in any PR that changes an upstream file.

New fork-specific files (additive — `AGENTS.md`, `CLAUDE.md`, `docs/vg/*`,
`Tools/_VG/*`, `.github/workflows/vg-*.yml`, `.github/ISSUE_TEMPLATE/03_dev_task.yml`)
do not create conflict surface and are not required here.

## Modified files

| File | Change | Reason |
|------|--------|--------|
| `README.md` | Rebranded for vgstation14 | Project identity |
| `CONTRIBUTING.md` | Rebranded; links to divergence policy | Project identity |
| `SECURITY.md` | Rebranded contact info | Project identity |
| `.github/workflows/no-submodule-update.yml` | Skips `automated/upstream-sync*` branches | Upstream sync PRs must bump the RobustToolbox submodule |

## Deleted files

| File | Reason |
|------|--------|
| `.github/workflows/publish.yml` | Deploys to Space Wizards infrastructure; unused by the fork |
| `.github/workflows/publish-testing.yml` | Deploys to Space Wizards infrastructure; active daily cron |
| `.github/workflows/build-docfx.yml` | Builds the Space Wizards docs site |
| `.github/workflows/benchmarks.yml` | Runs benchmarks on Space Wizards infrastructure (SSH, their secrets); active daily cron |
| `CODE_OF_CONDUCT.md` | Needs a full rewrite; TBD completion |
