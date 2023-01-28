using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed class PainLocalModifierComponent : Component
{
    [DataField("modifier", required: true)]
    public FixedPoint2 Modifier;
}
