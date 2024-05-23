using Content.Shared.FixedPoint;
using Content.Shared.Medical.Pain.Components;

namespace Content.Shared.Medical.Pain.Events;


[ByRefEvent]
public record struct PainChangedEvent(Entity<NervesComponent> NervousSystem, FixedPoint2 PainDelta);
