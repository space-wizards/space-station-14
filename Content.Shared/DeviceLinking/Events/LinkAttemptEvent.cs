using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Events;

/// <summary>
/// Raised before a link is made between two entities
/// allowing to cancel it, for example if the user doesn't have access.
/// </summary>
/// <param name="User">The user that is trying to link devices together.</param>
/// <param name="Source">The source device entity.</param>
/// <param name="SourcePort">Source's device port.</param>
/// <param name="Sink">The sink device entity.</param>
/// <param name="SinkPort">Sink's device port.</param>
/// <param name="Cancelled">Whether the link was canceled.</param>
[ByRefEvent]
public record struct LinkAttemptEvent(
    EntityUid? User,
    EntityUid Source,
    ProtoId<SourcePortPrototype> SourcePort,
    EntityUid Sink,
    ProtoId<SinkPortPrototype> SinkPort,
    bool Cancelled = false);
