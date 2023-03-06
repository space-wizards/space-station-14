using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Pain.Components;

[RegisterComponent, NetworkedComponent]
public sealed class PainThresholdsComponent : Component
{
    [DataField("painModifier")] public FixedPoint2 PainModifier = 1.0f;

    [DataField("rawPain")] public FixedPoint2 RawPain = 0f;

    [DataField("thresholds", required: true)]
    //Thresholds of pain mapped to consciousness damage values
    public SortedDictionary<FixedPoint2, FixedPoint2> Thresholds = new();
}
