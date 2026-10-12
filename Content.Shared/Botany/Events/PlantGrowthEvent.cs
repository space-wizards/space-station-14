using Robust.Shared.Prototypes;

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
public record struct PlantHarvestAttemptEvent(EntityUid User, EntityUid Target, EntityUid? Used = null, bool Cancelled = false);

/// <summary>
/// Event raised after a plant has been harvested.
/// </summary>
[ByRefEvent]
public readonly record struct PlantHarvestedEvent(EntityUid User, EntityUid Target);

/// <summary>
/// Raised after processing a plant's random mutations.
/// </summary>
[ByRefEvent]
public readonly record struct PlantMutationsChangedEvent(EntityUid Plant);

/// <summary>
/// Raised when a plant entity is replaced by a new species entity.
/// </summary>
[ByRefEvent]
public readonly record struct PlantSpeciesChangedEvent(EntityUid OldPlant, EntityUid NewPlant, EntProtoId NewPrototype);
