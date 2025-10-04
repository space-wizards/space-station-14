using Content.Shared.Damage;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent]
public sealed partial class WoundableComponent : Component;

/// <summary>
/// Raised on an entity to determine how much of its damage comes from wounds
/// </summary>
[ByRefEvent]
public record struct WoundGetDamageEvent(DamageSpecifier Accumulator);

/// <summary>
/// Raised before damage is applied to a Damageable but after applying modifiers
/// </summary>
[ByRefEvent]
public record struct BeforeDamageCommitEvent(DamageSpecifier Damage, bool ForceRefresh);

/// <summary>
/// Raised when the values for a damage overlay may have changed
/// </summary>
[ByRefEvent]
public record struct bPotentiallyUpdateDamageOverlayEventb(EntityUid Target);
