using Content.Shared.Chemistry.Components;

namespace Content.Server.Nutrition.Events;

/// <summary>
/// Raised on the entity drinking. This is right before they actually transfer the solution into the stomach.
/// </summary>
/// <param name="Drink">The drink that is being drank.</param>
/// <param name="Solution">The solution that will be digested.</param>
[ByRefEvent]
public readonly record struct BeforeIngestDrinkEvent(EntityUid Drink, Solution Solution);
