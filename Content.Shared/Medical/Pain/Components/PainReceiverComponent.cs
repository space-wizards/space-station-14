using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed class PainReceiverComponent : Component
{
    [DataField("painModifier")] public FixedPoint2 PainModifier = 1.0f;

    [DataField("rawPain")] public FixedPoint2 RawPain = 0f;

    public FixedPoint2 Pain => PainModifier * RawPain;
}
