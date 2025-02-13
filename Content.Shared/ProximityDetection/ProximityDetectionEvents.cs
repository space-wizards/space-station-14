using Content.Shared.ProximityDetection.Components;

namespace Content.Shared.ProximityDetection;

/// <summary>
/// Raised to determine if proximity sensor can detect an entity.
/// </summary>
[ByRefEvent]
public struct ProximityDetectionAttemptEvent(float distance, Entity<ProximityDetectorComponent> detector, EntityUid target)
{
    public bool Cancelled;
    public readonly float Distance = distance;
    public readonly Entity<ProximityDetectorComponent> Detector = detector;
    public readonly EntityUid Target = target;
}

/// <summary>
/// Raised when distance from proximity sensor to the target was updated.
/// </summary>
[ByRefEvent]
public readonly struct ProximityTargetUpdatedEvent(float distance, Entity<ProximityDetectorComponent> detector, EntityUid? target = null)
{
    public readonly float Distance = distance;
    public readonly Entity<ProximityDetectorComponent> Detector = detector;
    public readonly EntityUid? Target = target;
}

/// <summary>
/// Raised when proximity sensor got new target.
/// </summary>
[ByRefEvent]
public readonly struct NewProximityTargetEvent(float distance, Entity<ProximityDetectorComponent> detector, EntityUid? target = null)
{
    public readonly float Distance = distance;
    public readonly Entity<ProximityDetectorComponent> Detector = detector;
    public readonly EntityUid? Target = target;
}
