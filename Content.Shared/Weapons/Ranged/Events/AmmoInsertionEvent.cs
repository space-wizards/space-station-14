using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a <see cref="BallisticAmmoProviderComponent"/> or <see cref="RevolverAmmoProviderComponent"/> entity when ammo is inserted into it.
/// </summary>
/// <param name="Ammo">The ammo entity being inserted.</param>
[ByRefEvent]
public record struct AmmoInsertionEvent(EntityUid Ammo);
