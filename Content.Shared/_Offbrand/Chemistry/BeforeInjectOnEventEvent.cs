using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Chemistry;

[ByRefEvent]
public struct BeforeInjectOnEventEvent
{
    public FixedPoint2 InjectionAmount;
}
