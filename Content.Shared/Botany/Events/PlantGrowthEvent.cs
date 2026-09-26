namespace Content.Shared.Botany.Events;

/// <summary>
/// Event of plant growing ticking.
/// </summary>
[ByRefEvent]
public readonly record struct PlantGrowEvent(EntityUid Tray);

/// <summary>
/// Event raised when a harvest is attempted. Cancel to prevent the harvest.
/// </summary>
[ByRefEvent]
public record struct PlantHarvestAttemptEvent(EntityUid User, EntityUid Target, bool Cancelled = false);

/// <summary>
/// Event raised after a plant has been harvested.
/// </summary>
[ByRefEvent]
public readonly record struct PlantHarvestedEvent(EntityUid User, EntityUid Target);
