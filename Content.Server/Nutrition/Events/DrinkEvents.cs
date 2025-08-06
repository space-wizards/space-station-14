using Content.Shared.Chemistry.Components;

namespace Content.Server.Nutrition.Events;

/// <summary>
/// Raised on the entity drinking. This is right before they actually transfer the solution into the stomach.
/// </summary>
/// <param name="Drink">The drink that is being drank.</param>
/// <param name="Solution">The solution that will be digested.</param>
/// <param name="Forced">Whether the target was forced to drink the solution by somebody else.</param>
[ByRefEvent]
public record struct BeforeIngestDrinkEvent(EntityUid Drink, Solution Solution, bool Forced);
