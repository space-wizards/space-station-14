using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(WoundableSystem))]
public sealed partial class WoundComponent : Component
{
    /// <summary>
    /// The amount of damage this wound represents
    /// </summary>
    [DataField, AutoNetworkedField]
    public Damages Damage = new();

    /// <summary>
    /// The maximum amount of damage this wound can take
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaximumDamage;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan WoundedAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan CreatedAt;
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundableSystem))]
public sealed partial class WoundDescriptionComponent : Component
{
    /// <summary>
    /// The description to use for this wound. Highest threshold used.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, LocId> Descriptions;

    [DataField]
    public LocId BleedingModifier = "wound-bleeding-modifier";

    [DataField]
    public LocId TendedModifier = "wound-tended-modifier";
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundableSystem), typeof(SharedWoundableHealthAnalyzerSystem))]
public sealed partial class AnalyzableWoundComponent : Component
{
    /// <summary>
    /// The analyzer message to report for this wound. Highest threshold used.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, LocId> Descriptions;
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundableSystem))]
public sealed partial class PainfulWoundComponent : Component
{
    /// <summary>
    /// Coefficients for damage to pain
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> PainCoefficients;

    /// <summary>
    /// Coefficients for damage to initial pain
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> FreshPainCoefficients;

    [DataField]
    public double FreshPainDecreasePerSecond = 0.05d;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WoundableSystem))]
public sealed partial class HealableWoundComponent : Component
{
    /// <summary>
    /// Whether or not the wound can heal
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanHeal = true;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WoundableSystem))]
public sealed partial class TendableWoundComponent : Component
{
    /// <summary>
    /// Whether or not the wound has been tended
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Tended;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WoundableSystem))]
public sealed partial class ClampableWoundComponent : Component
{
    /// <summary>
    /// Whether or not the wound has been clamped
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Clamped;
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(WoundableSystem))]
public sealed partial class BleedingWoundComponent : Component
{
    /// <summary>
    /// Coefficient of damage type from this wound to bleeding rate
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, float> BleedingCoefficients;

    /// <summary>
    /// Coefficient of damage type from this wound to bleeding duration
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> BleedingDurationCoefficients;

    /// <summary>
    /// Wound must have at least this much damage to start bleeding
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 StartsBleedingAbove;

    /// <summary>
    /// Wound must be tended before bleeding ends if it has this much damage
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 RequiresTendingAbove;
}

/// <summary>
/// Raised on an entity to determine which of its wounds can take on the given damage
/// </summary>
[ByRefEvent]
public record struct GetWoundsWithSpaceEvent(List<EntityUid> Wounds, DamageSpecifier Damage);

/// <summary>
/// Raised on an entity to attempt to heal its wounds with the given damage
/// </summary>
[ByRefEvent]
public record struct HealWoundsEvent(DamageSpecifier Damage);

/// <summary>
/// Raised on an entity to get the sum total of pain
/// </summary>
[ByRefEvent]
public record struct GetPainEvent(FixedPoint2 Pain);

/// <summary>
/// Raised on an entity to get the sum total of heart strain
/// </summary>
[ByRefEvent]
public record struct GetStrainEvent(FixedPoint2 Strain);

/// <summary>
/// Raised on an entity to get the amount it should bleed
/// </summary>
[ByRefEvent]
public record struct GetBleedLevelEvent(float BleedLevel);

/// <summary>
/// Raised on an entity to modify the bleed level before committing to bleeding
/// </summary>
[ByRefEvent]
public record struct ModifyBleedLevelEvent(float BleedLevel);

/// <summary>
/// Raised on an entity's wounds to clamp them with the given probability
/// </summary>
[ByRefEvent]
public record struct ClampWoundsEvent(float Probability);
