namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
///     Raised directed on the gun entity when a muzzle flash is about to happen.
/// </summary>
/// <param name="Cancelled">If set to true, the muzzle flash will not be shown.</param>
[ByRefEvent]
public record struct GunMuzzleFlashAttemptEvent(bool Cancelled);
