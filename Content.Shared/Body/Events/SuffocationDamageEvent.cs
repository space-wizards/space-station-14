using Content.Shared.Damage;

namespace Content.Shared.Body.Events;

/// <summary>
/// Raised on an entity before they take asphyxiation damage.
/// </summary>
/// <param name="AsphyxationAmount">The amount of asphyxiation damage that will be taken.</param>
[ByRefEvent]
public record struct SuffocationDamageEvent(DamageSpecifier AsphyxationAmount);
