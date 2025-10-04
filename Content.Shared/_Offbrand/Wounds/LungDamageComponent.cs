using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(LungDamageSystem))]
public sealed partial class LungDamageComponent : Component
{
    /// <summary>
    /// The maximum amount of damage this entity's lungs can take
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxDamage;

    /// <summary>
    /// The current amount of accrued damage
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Damage;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(LungDamageSystem))]
public sealed partial class LungDamageAlertsComponent : Component
{
    /// <summary>
    /// The alert to display depending on the amount of lung damage. Highest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, ProtoId<AlertPrototype>> AlertThresholds;

    /// <summary>
    /// The alert category of the alerts.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertCategoryPrototype> AlertCategory;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype>? CurrentAlertThresholdState;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class LungDamageOnInhaledAirTemperatureComponent : Component
{
    /// <summary>
    /// The coefficient for how much damage is taken when the air temperature is below <see cref="TemperatureComponent"/>'s ColdDamageThreshold
    /// </summary>
    [DataField(required: true)]
    public float ColdCoefficient;

    /// <summary>
    /// The constant for how much damage is taken when the air temperature is below <see cref="TemperatureComponent"/>'s ColdDamageThreshold
    /// </summary>
    [DataField(required: true)]
    public float ColdConstant;

    /// <summary>
    /// The coefficient for how much damage is taken when the air temperature is below <see cref="TemperatureComponent"/>'s HeatDamageThreshold
    /// </summary>
    [DataField(required: true)]
    public float HeatCoefficient;

    /// <summary>
    /// The constant for how much damage is taken when the air temperature is below <see cref="TemperatureComponent"/>'s HeatDamageThreshold
    /// </summary>
    [DataField(required: true)]
    public float HeatConstant;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(LungDamageSystem))]
public sealed partial class PassiveLungDamageComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField(required: true)]
    public FixedPoint2 Damage;

    [DataField(required: true)]
    public FixedPoint2 DamageCap;
}

/// <summary>
/// Event raised when an entity is about to take a breath
/// </summary>
/// <param name="BreathVolume">The volume to breathe in.</param>
[ByRefEvent]
public record struct BeforeBreathEvent(float BreathVolume);

/// <summary>
/// Event raised when an entity successfully inhales a gas, before storing the gas internally
/// </summary>
/// <param name="Gas">The gas we're inhaling.</param>
[ByRefEvent]
public record struct BeforeInhaledGasEvent(GasMixture Gas);

/// <summary>
/// Event raised when an entity's lung damage changes
/// </summary>
[ByRefEvent]
public record struct AfterLungDamageChangedEvent;
