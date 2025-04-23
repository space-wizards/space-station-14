using Robust.Shared.Prototypes;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared.Starlight.Medical.Surgery.Events;

/// <summary>
///     Raised on the step entity.
/// </summary>
[ByRefEvent]
public record struct SurgeryStepEvent(EntityUid User, EntityUid Body, EntityUid Part, List<EntityUid> Tools)
{
    public required EntProtoId StepProto { get; init; }
    public required EntProtoId SurgeryProto { get; init; }
    public bool IsCancelled { get; set; }
}
[ByRefEvent]
public record struct SurgeryStepCompleteEvent(EntityUid User, EntityUid Body, EntityUid Part, List<EntityUid> Tools)
{
    public required EntProtoId StepProto { get; init; }
    public required EntProtoId SurgeryProto { get; init; }
    public required bool IsFinal { get; init; }
}
