namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised directed on the target entity when being attacked.
/// </summary>
[ByRefEvent]
public record struct GettingAttackedAttemptEvent(EntityUid Attacker, EntityUid? Weapon, bool Disarm, bool Cancelled = false);
