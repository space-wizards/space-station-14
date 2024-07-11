namespace Content.Shared.Damage.Events;

/// <summary>
/// Attempting to apply stamina damage on entity.
/// </summary>
[ByRefEvent]
public record struct StaminaDamageOnHitAttemptEvent(EntityUid Target, float Damage, bool Cancelled = false);
