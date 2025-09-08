using Content.Shared.Damage;

namespace Content.Shared.Body.Events;

/// <summary>
/// Raised on an entity before they take bloodloss damage from a lack of blood.
/// </summary>
/// <param name="BloodlossDamageAmount">The amount of bloodloss damage that will be taken.</param>
[ByRefEvent]
public record struct BloodlossDamageEvent(DamageSpecifier BloodlossDamageAmount);
