using Content.Shared.FixedPoint;
using Content.Shared.Medical.Pain.Components;

namespace Content.Shared.Medical.Pain.System;

[ByRefEvent]
public readonly record struct InflictPainEvent(
    PainThresholdsComponent PainThresholds,
    FixedPoint2 Pain,
    EntityUid? Inflicter = null,
    bool Canceled = false);


