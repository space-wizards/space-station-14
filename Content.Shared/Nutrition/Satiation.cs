using Content.Shared.Damage;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class Satiation
{
    /// <summary>
    /// The current satiation amount of the entity
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Current;

    /// <summary>
    /// The base amount at which <see cref="Current"/> decays.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseDecayRate = 0.01666666666f;

    /// <summary>
    /// The actual amount at which <see cref="Current"/> decays.
    /// Affected by <seealso cref="CurrentThreshold"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ActualDecayRate;

    /// <summary>
    /// The last threshold this entity was at.
    /// Stored in order to prevent recalculating
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SatiationThreashold LastThreshold;

    /// <summary>
    /// The current nutrition threshold the entity is at
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SatiationThreashold CurrentThreshold;


    /// <summary>
    /// Stored in order to prevent recalculating
    /// </summary>
    public DamageSpecifier? CurrentThresholdDamage;

    /// <summary>
    /// The current nutrition threshold the entity is at
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SatiationPrototype> Prototype;
}

[Serializable, NetSerializable]
public enum SatiationThreashold : byte
{
    Full = 1 << 3,
    Okay = 1 << 2,
    Concerned = 1 << 1,
    Desperate = 1 << 0,
    Dead = 0,
}
