using Robust.Shared.GameObjects;

namespace Content.Shared.BloodCult;

/// <summary>
/// Raised when a cult melee hit was blocked by an ally.
/// Cancelling prevents the default team-hit popup.
/// </summary>
/// <param name="User">The entity that was wielding the weapon.</param>
/// <param name="Weapon">The cult melee weapon entity.</param>
/// <param name="Cancelled">Set to true to prevent the default popup.</param>
[ByRefEvent]
public record struct BloodCultMeleeAllyBlockedAttemptEvent(EntityUid User, EntityUid Weapon, bool Cancelled = false);

/// <summary>
/// Raised when a cult melee hit was blocked by a chaplain (CultResistant).
/// Cancelling prevents the default repel popup, holy sound, and weapon throw.
/// </summary>
/// <param name="User">The entity that was wielding the weapon.</param>
/// <param name="Weapon">The cult melee weapon entity.</param>
/// <param name="Cancelled">Set to true to prevent the default popup, sound, and throw.</param>
[ByRefEvent]
public record struct BloodCultMeleeChaplainBlockedAttemptEvent(EntityUid User, EntityUid Weapon, bool Cancelled = false);
