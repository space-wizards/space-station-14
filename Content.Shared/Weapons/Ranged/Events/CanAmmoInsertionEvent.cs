using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a <see cref="BallisticAmmoProviderComponent"/> or <see cref="RevolverAmmoProviderComponent"/> entity before ammo is inserted into it.
/// </summary>
/// <param name="Cancelled">If true, cancels the ammo insertion.</param>
[ByRefEvent]
public record struct CanAmmoInsertionEvent(bool Cancelled = false);
