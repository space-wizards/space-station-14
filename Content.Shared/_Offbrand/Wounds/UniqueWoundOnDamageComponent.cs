using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(UniqueWoundOnDamageSystem))]
public sealed partial class UniqueWoundOnDamageComponent : Component
{
    [DataField(required: true)]
    public List<UniqueWoundSpecifier> Wounds;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class UniqueWoundSpecifier
{
    /// <summary>
    /// The damage type this unique wound happens to
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<DamageTypePrototype>> DamageTypes;

    /// <summary>
    /// The minimum damage required to apply this wound from the delta alone
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinimumDamage;

    /// <summary>
    /// The minimum overall damage of the DamageTypes required to apply this wound even if the delta is not high enough
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinimumTotalDamage;

    /// <summary>
    /// The wound to inflict
    /// </summary>
    [DataField(required: true)]
    public EntProtoId WoundPrototype;

    /// <summary>
    /// The damages to inflict it with
    /// </summary>
    [DataField(required: true)]
    public Damages WoundDamages;

    /// <summary>
    /// The probability coefficient of the amount of incoming damage
    /// </summary>
    [DataField(required: true)]
    public double DamageProbabilityCoefficient;

    /// <summary>
    /// The probability constant of the amount of incoming damage
    /// </summary>
    [DataField(required: true)]
    public double DamageProbabilityConstant;
}
