using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Events;

/// <summary>
/// Raised on a source port that got disconnected.
/// </summary>
/// <param name="Port">Source port that was disconnected on this entity.</param>
[ByRefEvent]
public readonly record struct SourcePortDisconnectedEvent(ProtoId<SourcePortPrototype> Port);
