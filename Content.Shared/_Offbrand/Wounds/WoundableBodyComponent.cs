using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
public sealed partial class WoundableBodyComponent : Component;

/// <summary>
/// Raised on an entity to determine how much of its damage comes from wounds
/// </summary>
[ByRefEvent]
public record struct WoundGetDamageEvent(DamageSpecifier Accumulator, DamageSpecifier? Tended);

/// <summary>
/// Raised when the values for a damage overlay may have changed
/// </summary>
[ByRefEvent]
public record struct PotentiallyUpdateDamageOverlayEvent(EntityUid Target);
