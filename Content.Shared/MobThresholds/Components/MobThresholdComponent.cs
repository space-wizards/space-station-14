using Content.Shared.FixedPoint;
using Content.Shared.MobThresholds.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.MobThresholds.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MobThresholdSystem))]
public sealed class MobThresholdComponent : Component
{
    [DataField("MobStatesThresholds", required:true)]public SortedDictionary<FixedPoint2, MobState.MobState> Thresholds = new();

    public Dictionary<MobState.MobState, FixedPoint2> ThresholdReverseLookup = new();

    public MobState.MobState CurrentThresholdState;
}

[Serializable, NetSerializable]
public sealed class MobThresholdComponentState : ComponentState
{
    public SortedDictionary<FixedPoint2, MobState.MobState> Thresholds;
    public MobState.MobState CurrentThresholdState;
    public MobThresholdComponentState(MobState.MobState currentThresholdState,
        SortedDictionary<FixedPoint2, MobState.MobState> thresholds)
    {
        CurrentThresholdState = currentThresholdState;
        Thresholds = thresholds;
    }

}
