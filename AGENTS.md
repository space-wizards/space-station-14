# vgstation14 — Agent & Contributor Guide

vgstation14 is a fork of [Space Station 14](https://github.com/space-wizards/space-station-14)
(C# / RobustToolbox), maintained by the /vg/station13 community.

Read this before making any change. The full rules are in `docs/vg/`.

## Golden rule

**Additive work goes in `_VG/`. Never edit upstream files except as a registered
exception.**

- New features → new files under `_VG/` folders (`Content.Shared/_VG/`,
  `Resources/Prototypes/_VG/`, etc.). C# namespaces: `Content.Shared._VG.*`.
- Editing or deleting an existing upstream file → record it in
  `docs/vg/divergence-registry.md` **in the same PR**, or CI (`VG: Divergence
  check`) will fail the PR.
- See `docs/vg/divergence-policy.md` for the full contract and
  `docs/vg/design-pillars.md` for why.

## Porting from vgstation13

vgstation14 brings content over from [vgstation13](https://github.com/vgstation-coders/vgstation13),
the /vg/station13 community's original BYOND/SS13 codebase. When porting a
feature:

- Use the [vgstation13 wiki](https://ss13.moe/wiki/index.php/Main_Page) as the
  style and content guide — it documents how features look, behave, and are
  balanced.
- vgstation13 is BYOND/DM; SS14 is C#/ECS. Port the *concept and design* and
  re-implement it as new files in a `_VG/` zone — never translate code
  line-for-line.
- Cite the relevant wiki page (or repo source) in the issue's "vg13 reference"
  field.

## Build & test

Requires the .NET 10 SDK (`10.0.100`).

```bash
python RUN_THIS.py                                   # first-time setup
dotnet build                                         # build (CI: --configuration DebugOpt)
dotnet test Content.Tests/Content.Tests.csproj
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj
dotnet run --project Content.Server                  # run a local server
dotnet run --project Content.Client                  # run a local client
```

## Pull requests

- Branch from `master`; never open a PR from your `master` branch.
- One PR = one issue-sized change.
- Label the PR `tier:additive` or `tier:upstream-edit`.
- CI (`Build & Test Debug`, `VG: Divergence check`) must pass.

## Agentic development

vgstation14 uses agentic development. Contributions follow this guide and the
divergence policy regardless of how they were produced.
