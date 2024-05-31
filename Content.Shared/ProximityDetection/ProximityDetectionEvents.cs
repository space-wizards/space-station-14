using Content.Shared.FixedPoint;
using Content.Shared.ProximityDetection.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.ProximityDetection;

[ByRefEvent]
public record struct ProximityDetectionAttemptEvent(bool Cancel, FixedPoint2 Distance, Entity<ProximityDetectorComponent> Detector);

[ByRefEvent]
public record struct ProximityTargetUpdatedEvent(ProximityDetectorComponent Detector, EntityUid? Target, FixedPoint2 Distance);

[ByRefEvent]
public record struct NewProximityTargetEvent(ProximityDetectorComponent Detector, EntityUid? Target);



