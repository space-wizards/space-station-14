using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed class PainInflicterComponent : Component
{
    [DataField("pain", required: true)] public FixedPoint2 Pain;
}
