# vgstation14 — Divergence Policy

This is the operational contract that keeps upstream merges cheap. Every
contributor and every agent follows it.

## The golden rule

> **Additive work goes in `_VG/`. Upstream files are not edited except as a
> registered exception.**

## Additive zones

New content — every /vg/station13 feature port — lives in `_VG/`-prefixed
folders, touching zero upstream files:

- `Content.Shared/_VG/`, `Content.Server/_VG/`, `Content.Client/_VG/`
- `Resources/Prototypes/_VG/`, `Resources/Textures/_VG/`, `Resources/Audio/_VG/`
- C# namespaces: `Content.Shared._VG.*`, `Content.Server._VG.*`, etc.

Process docs under `docs/vg/` and `docs/superpowers/` are also additive zones.

## Registered upstream edits

Some changes genuinely cannot be additive (branding, the character-creation UI
filter, CI workflows). These are allowed **only** when:

1. The change is as small and localized as possible.
2. The edited file is recorded in `divergence-registry.md` (file, change,
   reason).

The `VG: Divergence check` CI job fails any PR that modifies or deletes a file
outside the additive zones without updating `divergence-registry.md`.

## Conflict tiers

Every issue and PR is one of:

- **`tier:additive`** — touches only `_VG/` zones. Parallelizes safely.
- **`tier:upstream-edit`** — touches a registered upstream file. Serialized,
  maintainer-reviewed.

## Upstream sync

`upstream` = `space-wizards/space-station-14`. The `VG: Upstream sync` workflow
opens a merge PR weekly. Conflicts land almost entirely in files listed in
`divergence-registry.md`.

## Hard fork (trigger: TBD)

vgstation14 will eventually stop tracking upstream and take full creative
control. **The trigger is not yet defined** and will be decided by a vote of
active collaborators. Until that vote, the soft-fork rules above are strict.
