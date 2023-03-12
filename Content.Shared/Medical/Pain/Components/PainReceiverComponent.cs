using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed class PainReceiverComponent : Component
{
    [DataField("painModifier")] public FixedPoint2 PainModifier = 1.0f;

    [DataField("rawPain")] public FixedPoint2 RawPain = 0f;

    //avoid changing this at runtime or bad shit will happen
    [DataField("maxPain")] public FixedPoint2 MaxPain = 100f;

    public FixedPoint2 ConsciousnessDamage = 0.0f;

    public FixedPoint2 Pain => RawPain * PainModifier;
}

[Serializable, NetSerializable]
public sealed class PainReceiverComponentState : ComponentState
{
    public FixedPoint2 PainModifier;
    public FixedPoint2 RawPain;
    public FixedPoint2 MaxPain;
    public FixedPoint2 ConsciousnessDamage;
    public PainReceiverComponentState(FixedPoint2 painModifier, FixedPoint2 rawPain, FixedPoint2 maxPain, FixedPoint2 consciousnessDamage)
    {
        PainModifier = painModifier;
        RawPain = rawPain;
        MaxPain = maxPain;
        ConsciousnessDamage = consciousnessDamage;
    }
}
