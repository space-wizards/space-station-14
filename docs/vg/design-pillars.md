# vgstation14 — Design Pillars

vgstation14 is a fork of [Space Station 14](https://github.com/space-wizards/space-station-14),
created and maintained by the /vg/station13 community as the successor to
[vgstation13](https://github.com/vgstation-coders/vgstation13), its BYOND-based
SS13 codebase.

These pillars guide every contribution and design decision.

## 1. Stay mergeable (soft-fork era)

While we track upstream, divergence is the enemy of cheap merges. Keep the fork
as close to `space-wizards/space-station-14` as possible. All divergence is
deliberate, isolated, and recorded.

## 2. Curated content

vgstation14 ships a deliberately curated content set. Some upstream cosmetic
content and some character-creation options are not included. What is removed
and why is a scoping decision recorded alongside the work; it is not open-ended.

## 3. Additive porting

Features brought over from /vg/station13 are *re-implemented* as new content in
isolated `_VG/` modules — never by editing upstream files. A feature that cannot
be built additively is not a soft-fork feature.

## 4. Low drama

This is a video game. Keep development focused on gameplay. Process and policy
exist to reduce friction, not create it.

## Eras

- **Soft-fork era (now):** track upstream continuously; pillars 1–3 are strict.
- **Hard-fork era (later):** upstream tracking stops and full creative control
  begins. The trigger is decided by a vote of active collaborators — see
  `divergence-policy.md`.
