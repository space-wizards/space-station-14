using Content.Shared.FixedPoint;
using Content.Shared.Medical.Pain.Components;

namespace Content.Shared.Medical.Pain.Events;


[ByRefEvent]
public record struct PainChangedEvent(Entity<NervousSystemComponent> NervousSystem, FixedPoint2 PainDelta);
