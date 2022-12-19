using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.MentalState.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MentalStateEffectorComponent : Component
{
    [DataField("mentalStateModifier")] public FixedPoint2 ConsciousnessModifier = 1.0;

    [DataField("mentalStateOffset")] public FixedPoint2 ConsciousnessOffset = 0;

    [DataField("mentalStateLimit")] public FixedPoint2 ConsciousnessLimit = -1;
}
