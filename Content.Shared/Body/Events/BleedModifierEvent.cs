namespace Content.Shared.Body.Events;

/// <summary>
/// Raised on an entity before they bleed to modify the amount.
/// </summary>
/// <param name="BleedAmount">The amount of blood the entity will lose.</param>
/// <param name="BleedReductionAmount">The amount of bleed reduction that will happen.</param>
[ByRefEvent]
public record struct BleedModifierEvent(float BleedAmount, float BleedReductionAmount);
