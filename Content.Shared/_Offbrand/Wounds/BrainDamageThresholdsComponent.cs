using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BrainDamageThresholdsSystem))]
public sealed partial class BrainDamageThresholdsComponent : Component
{
    /// <summary>
    /// Which mob state to use, given brain damage. Highest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, MobState> DamageStateThresholds = new();

    /// <summary>
    /// Which mob state to use, given brain oxygen. Lowest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, MobState> OxygenStateThresholds = new();

    [DataField, AutoNetworkedField]
    public MobState CurrentState = MobState.Alive;

    /// <summary>
    /// Which status effect to apply, given brain damage. Highest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, EntProtoId> DamageEffectThresholds = new();

    [DataField, AutoNetworkedField]
    public EntProtoId? CurrentDamageEffect;

    /// <summary>
    /// Which status effect to apply, given brain damage. Lowest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, EntProtoId> OxygenEffectThresholds = new();

    [DataField, AutoNetworkedField]
    public EntProtoId? CurrentOxygenEffect;

    /// <summary>
    /// Damage icons to show on medical HUDs when the brain is alive and non-critical.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<HealthIconPrototype>> AliveDamageIcons;

    /// <summary>
    /// Damage icons to show on medical HUDs when the brain is critical.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<HealthIconPrototype>> CriticalDamageIcons;

    /// <summary>
    /// Damage icons to show on medical HUDs when the brain is dead.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<HealthIconPrototype> DeadIcon;

    /// <summary>
    /// The alert to display depending on the amount of brain damage. Highest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, ProtoId<AlertPrototype>> DamageAlertThresholds;

    /// <summary>
    /// The alert category of the alerts.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertCategoryPrototype> DamageAlertCategory;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype>? CurrentDamageAlertThresholdState;
}
