using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed class PainThresholdsComponent : Component
{
    [DataField("painModifier")] public FixedPoint2 PainModifier = 1.0f;

    [DataField("rawPain")] public FixedPoint2 RawPain = 0f;

    [DataField("thresholds", required: true)]
    //Thresholds of pain mapped to consciousness damage values
    public SortedDictionary<FixedPoint2, FixedPoint2> Thresholds = new();
    public FixedPoint2 CurrentThreshold = 0; //this does not need to be netsynced
}

[Serializable, NetSerializable]
public sealed class PainThresholdComponentState : ComponentState
{
    public FixedPoint2 PainModifier;
    public FixedPoint2 RawPain;
    public Dictionary<FixedPoint2, FixedPoint2> Thresholds;

    public PainThresholdComponentState(FixedPoint2 painModifier, FixedPoint2 rawPain, Dictionary<FixedPoint2, FixedPoint2> thresholds)
    {
        PainModifier = painModifier;
        RawPain = rawPain;
        Thresholds = thresholds;
    }
}
