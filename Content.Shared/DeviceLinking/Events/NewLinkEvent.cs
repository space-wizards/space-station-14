using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Events;

[ByRefEvent]
public readonly record struct NewLinkEvent(
    EntityUid? User,
    EntityUid Source,
    ProtoId<SourcePortPrototype> SourcePort,
    EntityUid Sink,
    ProtoId<SinkPortPrototype> SinkPort);
