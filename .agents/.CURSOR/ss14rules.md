# SS14 rules — warnings and errors to avoid

CarpFinder code should compile without these. Do not patch `RobustToolbox/` to silence them. That fork is shared with non-modded clients.

## RA0051 — `[Dependency]` must not be `readonly`

Robust assigns `[Dependency]` fields after the object is constructed. `readonly` will become a compile error.

```csharp
[Dependency] private CFDyingSystem _dying = default!;
```

Do not write `[Dependency] private readonly`.

## No collection expressions for `List<T>`

The client sandbox rejects `CollectionsMarshal.SetCount`. A collection expression that builds a `List<T>` (`return [item, item];`) compiles to that call and the client dies at startup with `Assembly Content.Client failed type checks`.

Build the list with `new List<T>()` and `Add`. Array expressions (`new[] { "a" }`) are fine.

## Do not sum SS14 damage

`DamageableSystem.GetTotalDamage`, `GetAllDamage`, and `GetDamagePerGroup` are obsolete: do not treat damage as one number. `DamageableComponent.TotalDamage` and `Damage` are access-restricted to `DamageableSystem`.

Pathfinder hit points are the source of truth. Write the SS14 bar with `DamageableSystem.SetDamage` (one `Blunt` value derived from CF HP and mob thresholds). Do not read the current total and add a delta.

## Do not dispose UI controls

`Control.Dispose()` and `Control.Dispose(bool)` are obsolete (`CS0618`). Overriding `Dispose(bool)` also warns `CS0672`.

Remove a control from the UI tree. Do not add an empty `Dispose` override that only calls `base.Dispose`. The lobby editor overlay (`HumanoidProfileEditor` under `_CF`) follows this.

## Event subscriptions

One directed component + event pair. A second subscriber throws `Duplicate Subscriptions` at startup.

- `MobStateComponent` + `MobStateChangedEvent` is already taken by `SharedStunSystem`. Subscribe to the broadcast, not the component.
- `MobStateChangedEvent` has no `[ByRefEvent]`. Existing handlers are by-value. A `ref` handler throws `Attempted to subscribe by-ref and by-value to the same broadcast event`.
- `ProjectileComponent` + `BeforeProjectileHitEvent` is already handled by `CFStrikeSystem`. Do not add another subscriber. Party blocks and strike resolution belong in that handler.
- Melee friendly-fire cancels `GettingAttackedAttemptEvent` (raised from `CanAttack`) before the strike runs.

## Build

- Never build `Content.Shared` alone. Build `Content.Server` and/or `Content.Client`.
- `MSB3027` means the client or server is locking DLLs. Close them, then rebuild.

## Leave these alone

- `NU1903` on ImageSharp and `CS1668` for a missing `C:\sqlite\lib` path are environment / package warnings, not CF code.
- Warnings whose source file is under `RobustToolbox/` stay upstream.
