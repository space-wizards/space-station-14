using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Mobs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MobThresholdSystem))]
public sealed class MobThresholdsComponent : Component
{
    [DataField("thresholds", required:true), AutoNetworkedField(true)]
    public SortedDictionary<FixedPoint2, MobState> Thresholds = new();

    [DataField("triggersAlerts"), AutoNetworkedField]
    public bool TriggersAlerts = true;

    [DataField("currentThresholdState"), AutoNetworkedField]
    public MobState CurrentThresholdState;

    /// <summary>
    /// Whether or not this entity can be revived out of a dead state.
    /// </summary>
    [DataField("allowRevives"), AutoNetworkedField]
    public bool AllowRevives;
}
