using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Events;

/// <summary>
/// Raised on the target plant entity when a swab cross-pollination happens.
/// Carries the pollen snapshot entity and the pollen prototype id.
/// </summary>
[ByRefEvent]
public readonly record struct PlantCrossPollinateEvent(EntityUid PollenData, EntProtoId? PollenProtoId);
