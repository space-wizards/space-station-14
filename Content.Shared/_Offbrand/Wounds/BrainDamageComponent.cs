using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BrainDamageSystem))]
public sealed partial class BrainDamageComponent : Component
{
    /// <summary>
    /// The maximum amount of damage this entity's brain can take
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxDamage;

    /// <summary>
    /// The current amount of accrued damage
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Damage;

    /// <summary>
    /// The maximum amount of oxygen this entity's brain can store
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxOxygen;

    /// <summary>
    /// The current amount of stored brain oxygen
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Oxygen;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(BrainDamageSystem))]
public sealed partial class BrainDamageOxygenationComponent : Component
{
    /// <summary>
    /// The thresholds for how much damage will occur from inadequate oxygenation.
    /// - Chance: how much of a chance this threshold will have to trigger
    /// - AtMost: this threshold will not trigger if there is more than this amount of brain damage
    /// - Amount: how much brain damage will be dealt if it triggers
    /// Lowest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, (double Chance, (FixedPoint2 AtMost, FixedPoint2 Amount) Data)> OxygenationDamageThresholds;

    /// <summary>
    /// The thresholds for how much oxygen will be depleted from inadequate oxygenation.
    /// - Chance: how much of a chance this threshold will have to trigger
    /// - Amount: how much oxygen will be depleted if it triggers
    /// Lowest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, (double Chance, FixedPoint2 Amount)> OxygenDepletionThresholds;

    /// <summary>
    /// How much oxygen will regenerate if none of the <see cref="OxygenDepletionThresholds" /> trigger
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 OxygenRegeneration;

    /// <summary>
    /// How much brain damage can be accumulated before passive healing will stop if none of the <see cref="OxygenationDamageThresholds" /> trigger
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxPassivelyHealableDamage;

    /// <summary>
    /// How much damage will be healed if the brain is capable of healing
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 DamageHealing;

    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan? LastUpdate;
}

/// <summary>
/// Raised on an entity to modify the chance for brain damage to be dealt
/// </summary>
[ByRefEvent]
public record struct BeforeDealBrainDamage(double Chance);

/// <summary>
/// Raised on an entity to modify the chance for brain damage to be dealt
/// </summary>
[ByRefEvent]
public record struct BeforeDepleteBrainOxygen(double Chance);

/// <summary>
/// Raised on an entity to determine if it should heal brain damage
/// </summary>
[ByRefEvent]
public record struct BeforeHealBrainDamage(bool Heal);

/// <summary>
/// Raised on an entity after its brain oxygen has changed
/// </summary>
[ByRefEvent]
public record struct AfterBrainOxygenChanged;

/// <summary>
/// Raised on an entity after its brain damage has changed
/// </summary>
[ByRefEvent]
public record struct AfterBrainDamageChanged;
