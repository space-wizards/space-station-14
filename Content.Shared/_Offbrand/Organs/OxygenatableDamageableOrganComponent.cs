using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(OxygenatableOrganSystem))]
public sealed partial class OxygenatableDamageableOrganComponent : Component
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

    /// <summary>
    /// How large a stage of damage is.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 DamageStageSize;

    /// <summary>
    /// How much you can heal within a stage of damage.
    /// </summary>
    /// <remarks>
    /// <see cref="DamageStageSize" /> - this should be less than <see cref="DamageHealing" />
    /// </remarks>
    [DataField(required: true)]
    public FixedPoint2 DamageStageMaximumHealing;

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
/// Raised on a body to modify the chance for damage to be dealt
/// </summary>
[ByRefEvent]
public record struct BeforeDealOrganOxygenDamage(double Chance, Entity<OrganComponent> Organ);

/// <summary>
/// Raised on a body to modify the chance for organ damage to be dealt
/// </summary>
[ByRefEvent]
public record struct BeforeDepleteOrganOxygen(double Chance, Entity<OrganComponent> Organ);

/// <summary>
/// Raised on a body to determine if it should heal organ damage
/// </summary>
[ByRefEvent]
public record struct BeforeHealOrganOxygenDamage(bool Heal, Entity<OrganComponent> Organ);
