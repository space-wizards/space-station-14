using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HeartDamageOnDamageSystem))]
public sealed partial class HeartDamageOnDamageComponent : Component
{
    [DataField(required: true)]
    public List<OrganDamageThresholdSpecifier> Thresholds;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class OrganDamageThresholdSpecifier
{
    /// <summary>
    /// The damage type this unique wound happens to
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<DamageTypePrototype>> DamageTypes;

    /// <summary>
    /// The minimum overall damage of the DamageTypes required to apply organ damage
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MinimumTotalDamage;

    /// <summary>
    /// The factor for converting incoming damage into organ damage
    /// </summary>
    [DataField(required: true)]
    public double ConversionFactor;
}
