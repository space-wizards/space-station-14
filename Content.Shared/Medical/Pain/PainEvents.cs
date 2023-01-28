using Content.Shared.FixedPoint;

namespace Content.Shared.Medical.Pain;


[ByRefEvent]
public record struct PainUpdatedEvent(FixedPoint2 OldPain);

[ByRefEvent]
public record struct PainOverloadEvent();
