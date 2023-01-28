using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed class PainReceiverComponent : Component
{
    [DataField("limit", required: true)] public FixedPoint2 Limit;

    [DataField("basePain")] public FixedPoint2 BasePain;

    public FixedPoint2 Pain;

    [DataField("modifier")] public FixedPoint2 Modifier = 1.0;
}
