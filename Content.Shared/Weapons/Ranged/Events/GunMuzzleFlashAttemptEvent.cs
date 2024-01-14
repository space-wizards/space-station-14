namespace Content.Shared.Weapons.Ranged.Events;

[ByRefEvent]
public record struct GunMuzzleFlashAttemptEvent(bool Cancelled);
