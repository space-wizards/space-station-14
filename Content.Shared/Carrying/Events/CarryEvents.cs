using Robust.Shared.GameObjects;

namespace Content.Shared.Carrying.Events;

/// <summary>
/// Raised on both carrier and target before carry starts. Can be cancelled.
/// </summary>
[ByRefEvent]
public record struct CarryAttemptEvent(EntityUid Carrier, EntityUid Target, bool Cancelled = false);

/// <summary>
/// Raised on both carrier and target after carry starts.
/// </summary>
[ByRefEvent]
public record struct CarryStartedEvent(EntityUid Carrier, EntityUid Target);

/// <summary>
/// Raised on both carrier and target when carry ends.
/// </summary>
[ByRefEvent]
public record struct CarryStoppedEvent(EntityUid Carrier, EntityUid Target);
