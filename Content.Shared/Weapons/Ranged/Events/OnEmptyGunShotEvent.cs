namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised directed on the gun when trying to fire it while it's out of ammo
/// </summary>
[ByRefEvent]
public record struct OnEmptyGunShotEvent(EntityUid User);
