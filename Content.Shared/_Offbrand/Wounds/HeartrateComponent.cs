using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HeartSystem))]
public sealed partial class HeartrateComponent : Component
{
    /// <summary>
    /// The damage type to use when computing oxygenation from the lungs
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DamageTypePrototype> AsphyxiationDamage;

    /// <summary>
    /// The amount of <see cref="AsphyxiationDamage" /> at which lung oxygenation is considered to be 0%
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 AsphyxiationThreshold;

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

    /// <summary>
    /// The coefficient for how much strain contributes to the blood circulation
    /// </summary>
    [DataField(required: true)]
    public FixedPoint4 CirculationStrainModifierCoefficient;

    /// <summary>
    /// The constant for how much strain contributes to the blood circulation
    /// </summary>
    [DataField(required: true)]
    public FixedPoint4 CirculationStrainModifierConstant;

    /// <summary>
    /// How much blood circulation there is when the heart is stopped
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 StoppedBloodCirculationModifier;

    /// <summary>
    /// Blood circulation will never go below this number from damage
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinimumDamageCirculationModifier;

    /// <summary>
    /// Shock will be divided by this much before contributing to strain
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 ShockStrainDivisor;

    /// <summary>
    /// How much reported blood pressure deviates
    /// </summary>
    [DataField(required: true)]
    public int BloodPressureDeviation;

    /// <summary>
    /// Base number for reported systolic blood pressure
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 SystolicBase;

    /// <summary>
    /// Base number for reported diastolic blood pressure
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 DiastolicBase;

    /// <summary>
    /// How much the reported heartrate deviates
    /// </summary>
    [DataField(required: true)]
    public int HeartRateDeviation;

    /// <summary>
    /// The base of the reported heartrate
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 HeartRateBase;

    /// <summary>
    /// Strain will be multiplied with this to contribute to the reported heartrate
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 HeartRateStrainFactor;

    /// <summary>
    /// Strain will be divided by this number before being multiplied to contribute to the reported heartrate
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 HeartRateStrainDivisor;

    /// <summary>
    /// The maximum amount of strain possible
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaximumStrain;

    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Strain = FixedPoint2.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan? LastUpdate;

    [DataField, AutoNetworkedField]
    public bool Running = true;

    /// <summary>
    /// The status effect to apply when the heart is not running
    /// </summary>
    [DataField(required: true)]
    public EntProtoId HeartStoppedEffect;
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
public sealed partial class HeartStopOnHypovolemiaComponent : Component
{
    /// <summary>
    /// How likely the heart is to stop when the volume threshold is dipped below
    /// </summary>
    [DataField(required: true)]
    public float Chance;

    /// <summary>
    /// The maximum volume at which the heart can stop from hypovolemia
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 VolumeThreshold;

    /// <summary>
    /// The warning issued by defibrillators if the heart is restarted with hypovolemia
    /// </summary>
    [DataField]
    public LocId Warning = "heart-defibrillatable-target-hypovolemia";
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
    public LocId Warning = "heart-defibrillatable-target-pain";
}

[RegisterComponent]
[Access(typeof(HeartSystem))]
public sealed partial class HeartStopOnBrainHealthComponent : Component
{
    /// <summary>
    /// How likely the heart is to stop when the brain health threshold is exceeded
    /// </summary>
    [DataField(required: true)]
    public float Chance;

    /// <summary>
    /// The minimum threshold at which the heart can stop from brain health
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Threshold;

    /// <summary>
    /// The warning issued by defibrillators if the heart is restarted with severe brain damage
    /// </summary>
    [DataField]
    public LocId Warning = "heart-defibrillatable-target-brain-damage";
}

/// <summary>
/// Raised on an entity to determine if the heart should stop
/// </summary>
[ByRefEvent]
public record struct HeartBeatEvent(bool Stop);

/// <summary>
/// Raised on an entity to determine its oxygenation modifier from air
/// </summary>
[ByRefEvent]
public record struct GetOxygenationModifier(FixedPoint2 Modifier);

/// <summary>
/// Raised on an entity to determine its circulation modifier when stopped
/// </summary>
[ByRefEvent]
public record struct GetStoppedCirculationModifier(FixedPoint2 Modifier);

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
