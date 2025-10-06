using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HeartSystem))]
public sealed partial class HeartrateComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Perfusion = 1f;

    [DataField, AutoNetworkedField]
    public float OxygenSupply = 1f;

    [DataField, AutoNetworkedField]
    public float OxygenDemand = 1f;

    [DataField, AutoNetworkedField]
    public float Compensation = 1f;

    [DataField(required: true)]
    public float RespiratoryRateCoefficient;

    [DataField(required: true)]
    public float RespiratoryRateConstant;

    [DataField(required: true)]
    public float CompensationCoefficient;

    [DataField(required: true)]
    public float CompensationConstant;

    [DataField(required: true)]
    public float CompensationStrainCoefficient;

    [DataField(required: true)]
    public float CompensationStrainConstant;

    [DataField]
    public float MinimumCardiacOutput = 0.005f;

    [DataField]
    public float MinimumVascularTone = 0.005f;

    [DataField]
    public float MinimumBloodVolume = 0.005f;

    [DataField]
    public float MinimumLungFunction = 0.005f;

    [DataField]
    public float MinimumRespiratoryRateModifier = 0.5f;

    [DataField]
    public float MinimumPerfusionEtco2Modifier = 0.5f;

    #region Damage
    /// <summary>
    /// The maximum amount of damage that this entity's heart can take
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxDamage;

    /// <summary>
    /// The current amount of damage that this entity's heart has
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Damage;

    /// <summary>
    /// How much damage to inflict on the heart depending on strain.
    /// - Chance: the chance to inflict damage
    /// - Amount: how much damage to inflict
    /// The highest amount is chosen.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, (double Chance, FixedPoint2 Amount)> StrainDamageThresholds;
    #endregion

    #region Heartstop
    [DataField, AutoNetworkedField]
    public bool Running = true;

    /// <summary>
    /// The status effect to apply when the heart is not running
    /// </summary>
    [DataField(required: true)]
    public EntProtoId HeartStoppedEffect;
    #endregion

    #region Fluff Numbers
    /// <summary>
    /// How much reported blood pressure deviates
    /// </summary>
    [DataField(required: true)]
    public int BloodPressureDeviation;

    /// <summary>
    /// Base number for reported systolic blood pressure
    /// </summary>
    [DataField(required: true)]
    public int SystolicBase;

    /// <summary>
    /// Base number for reported diastolic blood pressure
    /// </summary>
    [DataField(required: true)]
    public int DiastolicBase;

    /// <summary>
    /// How much the reported heartrate deviates
    /// </summary>
    [DataField(required: true)]
    public int HeartRateDeviation;

    /// <summary>
    /// The base of the reported heartrate at 100% perfusion
    /// </summary>
    [DataField(required: true)]
    public float HeartRateFullPerfusion;

    /// <summary>
    /// The base of the reported heartrate at 0% perfusion
    /// </summary>
    [DataField(required: true)]
    public float HeartRateNoPerfusion;

    /// <summary>
    /// The base of the reported etco2
    /// </summary>
    [DataField(required: true)]
    public float Etco2Base;

    /// <summary>
    /// The deviation of the reported etco2
    /// </summary>
    [DataField(required: true)]
    public int Etco2Deviation;

    /// <summary>
    /// The assumed time per normal breath in seconds
    /// </summary>
    [DataField(required: true)]
    public float RespiratoryRateNormalBreath;

    /// <summary>
    /// The name of the Etco2 vital
    /// </summary>
    [DataField(required: true)]
    public LocId Etco2Name;

    /// <summary>
    /// The name of the gas purged by Etco2
    /// </summary>
    [DataField(required: true)]
    public LocId Etco2GasName;

    /// <summary>
    /// The name of the Spo2 vital
    /// </summary>
    [DataField(required: true)]
    public LocId Spo2Name;

    /// <summary>
    /// The name of the gas circulated by Spo2
    /// </summary>
    [DataField(required: true)]
    public LocId Spo2GasName;
    #endregion

    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan? LastUpdate;

    #region VV Conveniences
    private HeartSystem _system => IoCManager.Resolve<IEntityManager>().System<HeartSystem>();

    [ViewVariables]
    private float VV000BloodVolume => _system.ComputeBloodVolume((Owner, this));

    [ViewVariables]
    private float VV001VascularTone => _system.ComputeVascularTone((Owner, this));

    [ViewVariables]
    private float VV002CardiacOutput => _system.CardiacOutput((Owner, this));

    [ViewVariables]
    private float VV003Perfusion => MathF.Min(VV000BloodVolume, MathF.Min(VV001VascularTone, VV002CardiacOutput));

    [ViewVariables]
    private float VV004LungFunction => _system.ComputeLungFunction((Owner, this));

    [ViewVariables]
    private float VV005GrossSupply => VV004LungFunction * VV003Perfusion;

    [ViewVariables]
    private float VV006Demand => _system.ComputeMetabolicRate((Owner, this));

    [ViewVariables]
    private float VV007Compensation => _system.ComputeCompensation((Owner, this), VV005GrossSupply, VV006Demand);

    [ViewVariables]
    private float VV008NetSupply => VV005GrossSupply * VV007Compensation;

    [ViewVariables]
    private float VV009Balance => VV008NetSupply / VV006Demand;

    [ViewVariables]
    private float VV010Strain => _system.Strain((Owner, this));

    [ViewVariables]
    private float VV011RespiratoryRateModifier => _system.ComputeRespiratoryRateModifier((Owner, this));
    #endregion
}

[RegisterComponent]
[Access(typeof(HeartSystem))]
public sealed partial class HeartDefibrillatableComponent : Component
{
    [DataField]
    public LocId TargetIsDead = "heart-defibrillatable-target-is-dead";
}

[RegisterComponent]
[Access(typeof(HeartSystem))]
public sealed partial class HeartStopOnHighStrainComponent : Component
{
    /// <summary>
    /// How likely the heart is to stop when the strain threshold is exceeded
    /// </summary>
    [DataField(required: true)]
    public float Chance;

    /// <summary>
    /// The minimum threshold at which the heart can stop from strain
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Threshold;

    /// <summary>
    /// The warning issued by defibrillators if the heart is restarted with high strain
    /// </summary>
    [DataField]
    public LocId Warning = "heart-defibrillatable-target-strain";
}

/// <summary>
/// Raised on an entity to determine the base vascular tone
/// </summary>
[ByRefEvent]
public record struct BaseVascularToneEvent(float Tone);

/// <summary>
/// Raised on an entity to determine modifiers to the vascular tone
/// </summary>
[ByRefEvent]
public record struct ModifiedVascularToneEvent(float Tone);

/// <summary>
/// Raised on an entity to determine the base lung function
/// </summary>
[ByRefEvent]
public record struct BaseLungFunctionEvent(float Function);

/// <summary>
/// Raised on an entity to determine modifiers to the lung function
/// </summary>
[ByRefEvent]
public record struct ModifiedLungFunctionEvent(float Function);

/// <summary>
/// Raised on an entity to determine the base cardiac output
/// </summary>
[ByRefEvent]
public record struct BaseCardiacOutputEvent(float Output);

/// <summary>
/// Raised on an entity to determine modifiers to the cardiac output
/// </summary>
[ByRefEvent]
public record struct ModifiedCardiacOutputEvent(float Output);

/// <summary>
/// Raised on an entity to determine the base metabolic rate
/// </summary>
[ByRefEvent]
public record struct BaseMetabolicRateEvent(float Rate);

/// <summary>
/// Raised on an entity to determine modifiers to the metabolic rate
/// </summary>
[ByRefEvent]
public record struct ModifiedMetabolicRateEvent(float Rate);

/// <summary>
/// Raised on an entity to determine modifiers to the respiratory rate
/// </summary>
[ByRefEvent]
public record struct ModifiedRespiratoryRateEvent(float Rate);

/// <summary>
/// Raised on an entity to update its respiratory rate
/// </summary>
[ByRefEvent]
public record struct ApplyRespiratoryRateModifiersEvent(float BreathRate, float PurgeRate);

/// <summary>
/// Raised on an entity to determine if the heart should stop
/// </summary>
[ByRefEvent]
public record struct HeartBeatEvent(bool Stop);

[ByRefEvent]
public record struct AfterStrainChangedEvent;

/// <summary>
/// Raised on an entity if the heart has stopped beating
/// </summary>
[ByRefEvent]
public record struct HeartStoppedEvent;

/// <summary>
/// Raised on an entity if the heart has started beating
/// </summary>
[ByRefEvent]
public record struct HeartStartedEvent;

/// <summary>
/// Raised on an entity to see if the defibrillator will say anything before defibrillation
/// </summary>
[ByRefEvent]
public record struct BeforeTargetDefibrillatedEvent(List<LocId> Messages);
