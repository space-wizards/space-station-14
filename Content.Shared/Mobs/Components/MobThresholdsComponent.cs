using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MobThresholdSystem))]
public sealed class MobThresholdsComponent : Component
{
    [DataField("thresholds", required:true)]
    public SortedDictionary<FixedPoint2, MobState> Thresholds = new();

    [DataField("triggersAlerts")]
    public bool TriggersAlerts = true;

    [DataField("currentThresholdState")]
    public MobState CurrentThresholdState;

    /// <summary>
    /// Whether or not this entity can be revived out of a dead state.
    /// </summary>
    [DataField("allowRevives")]
    public bool AllowRevives;
}

[Serializable, NetSerializable]
public sealed class MobThresholdsComponentState : ComponentState
{
    public Dictionary<FixedPoint2, MobState> UnsortedThresholds;

    public bool TriggersAlerts;

    public MobState CurrentThresholdState;

    public bool AllowRevives;

    public MobThresholdsComponentState(Dictionary<FixedPoint2, MobState> unsortedThresholds, bool triggersAlerts, MobState currentThresholdState, bool allowRevives)
    {
        UnsortedThresholds = unsortedThresholds;
        TriggersAlerts = triggersAlerts;
        CurrentThresholdState = currentThresholdState;
        AllowRevives = allowRevives;
    }
}
