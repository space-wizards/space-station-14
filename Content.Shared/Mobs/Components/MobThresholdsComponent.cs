using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MobThresholdSystem))]
public sealed partial class MobThresholdsComponent : Component
{
    [DataField("thresholds", required:true)]
    public SortedDictionary<FixedPoint2, MobState> Thresholds = new();

    [DataField("triggersAlerts")]
    public bool TriggersAlerts = true;

    [DataField("currentThresholdState")]
    public MobState CurrentThresholdState;

    /// <summary>
    /// The health alert that should be displayed for player controlled entities.
    /// Used for alternate health alerts (silicons, for example)
    /// </summary>
    [DataField("healthAlert")]
    public AlertType HealthAlert = AlertType.HumanHealth;

    [DataField("critAlert")]
    public AlertType CritAlert = AlertType.HumanCrit;

    [DataField("deadAlert")]
    public AlertType DeadAlert = AlertType.HumanDead;

    /// <summary>
    /// Whether or not this entity should display damage overlays (robots don't feel pain, black out etc.)
    /// </summary>
    [DataField("showOverlays")]
    public bool ShowOverlays = true;

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

    public AlertType HealthAlert = AlertType.HumanHealth;

    public AlertType CritAlert = AlertType.HumanCrit;

    public AlertType DeadAlert = AlertType.HumanDead;

    public bool ShowOverlays;

    public bool AllowRevives;

    public MobThresholdsComponentState(Dictionary<FixedPoint2, MobState> unsortedThresholds, bool triggersAlerts, MobState currentThresholdState,
        AlertType healthAlert, AlertType critAlert, AlertType deadAlert, bool showOverlays, bool allowRevives)
    {
        UnsortedThresholds = unsortedThresholds;
        TriggersAlerts = triggersAlerts;
        CurrentThresholdState = currentThresholdState;
        HealthAlert = healthAlert;
        CritAlert = critAlert;
        DeadAlert = deadAlert;
        ShowOverlays = showOverlays;
        AllowRevives = allowRevives;
    }
}
