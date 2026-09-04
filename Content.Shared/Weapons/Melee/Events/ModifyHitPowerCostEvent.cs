namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised to determine how much battery power this weapon uses on hit.
/// </summary>
/// <param name="Cost">The amount of power used per hit.</param>
[ByRefEvent]
public record struct ModifyHitPowerCostEvent(float Cost = 0);
