namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised directed on a weapon when attempt a melee attack.
/// </summary>
[ByRefEvent]
public record struct AttemptMeleeEvent(EntityUid User, EntityUid Weapon, bool Cancelled = false, string? Message = null); // 🌟Starlight🌟
