using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Events;

/// <summary>
/// Raised when a new device link was made successfully.
/// </summary>
/// <param name="User">The user that is trying to link devices together.</param>
/// <param name="Source">The source device entity.</param>
/// <param name="SourcePort">Source's device port.</param>
/// <param name="Sink">The sink device entity.</param>
/// <param name="SinkPort">Sink's device port.</param>
[ByRefEvent]
public readonly record struct NewLinkEvent(
    EntityUid? User,
    EntityUid Source,
    ProtoId<SourcePortPrototype> SourcePort,
    EntityUid Sink,
    ProtoId<SinkPortPrototype> SinkPort);
