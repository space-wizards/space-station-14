using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Events;

/// <summary>
/// Raised on a sink port that got disconnected.
/// </summary>
/// <param name="Port">Sink port that was disconnected on this entity.</param>
[ByRefEvent]
public readonly record struct SinkPortDisconnectedEvent(ProtoId<SinkPortPrototype> Port);
