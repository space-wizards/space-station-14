using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Events;

/// <summary>
/// Raised on the target plant entity when a swab cross-pollination happens.
/// Carries the pollen component snapshot and the pollen prototype id.
/// </summary>
[ByRefEvent]
public readonly record struct PlantCrossPollinateEvent(ComponentRegistry PollenData, EntProtoId? PollenProtoId);
