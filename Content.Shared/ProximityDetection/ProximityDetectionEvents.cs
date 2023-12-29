using Content.Shared.ProximityDetection.Components;

namespace Content.Shared.ProximityDetector;

[ByRefEvent]
public record struct ProximityDetectionAttemptEvent(bool ShouldDetect, float Distance, Entity<ProximityDetectorComponent> Detector);

[ByRefEvent]
public record struct ProximityDetectionEvent(float Distance, EntityUid FoundEntity);

[ByRefEvent]
public record struct ProximityDetectionNoTargetEvent;
