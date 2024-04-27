using Content.Shared.Damage;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.Nutrition.Components;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class Satiation
{
    /// <summary>
    /// The current satiation amount of the entity
    /// </summary>
    [DataField("current"), ViewVariables(VVAccess.ReadWrite)]
    public float Current;

    /// <summary>
    /// The base amount at which <see cref="Current"/> decays.
    /// </summary>
    [DataField("baseDecayRate"), ViewVariables(VVAccess.ReadWrite)]
    public float BaseDecayRate = 0.01666666666f;

    /// <summary>
    /// The actual amount at which <see cref="Current"/> decays.
    /// Affected by <seealso cref="CurrentThreshold"/>
    /// </summary>
    [DataField("actualDecayRate"), ViewVariables(VVAccess.ReadWrite)]
    public float ActualDecayRate;

    /// <summary>
    /// The last threshold this entity was at.
    /// Stored in order to prevent recalculating
    /// </summary>
    [DataField("lastThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public SatiationThreashold LastThreshold;

    /// <summary>
    /// The current nutrition threshold the entity is at
    /// </summary>
    [DataField("currentThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public SatiationThreashold CurrentThreshold;

    /// <summary>
    /// A dictionary relating SatiationThreshold to the amount of <see cref="Current"/> needed for each one
    /// </summary>
    [DataField("thresholds", customTypeSerializer: typeof(DictionarySerializer<SatiationThreashold, float>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<SatiationThreashold, float> Thresholds = new()
    {
        { SatiationThreashold.Full, 200.0f },
        { SatiationThreashold.Okay, 150.0f },
        { SatiationThreashold.Concerned, 100.0f },
        { SatiationThreashold.Desperate, 50.0f },
        { SatiationThreashold.Dead, 0.0f }
    };

    /// <summary>
    /// A dictionary relating SatiationThreshold to how much they modify <see cref="BaseDecayRate"/>.
    /// </summary>
    [DataField("thresholdDecayModifiers", customTypeSerializer: typeof(DictionarySerializer<SatiationThreashold, float>))]
    public Dictionary<SatiationThreashold, float> ThresholdDecayModifiers = new()
    {
        { SatiationThreashold.Full, 1.2f },
        { SatiationThreashold.Okay, 1f },
        { SatiationThreashold.Concerned, 0.8f },
        { SatiationThreashold.Desperate, 0.6f },
        { SatiationThreashold.Dead, 0.6f }
    };

    /// <summary>
    /// The amount of slowdown applied when an entity is at SatiationThreashhold.Desperate
    /// </summary>
    [DataField("slowdownModifier"), ViewVariables(VVAccess.ReadWrite)]
    public float SlowdownModifier = 0.75f;

    /// <summary>
    /// Damage dealt when at given threshold
    /// </summary>
    [DataField("thresholdDamage", customTypeSerializer: typeof(DictionarySerializer<SatiationThreashold, DamageSpecifier>))]
    public Dictionary<SatiationThreashold, DamageSpecifier> ThresholdDamage = new();

    /// <summary>
    /// Stored in order to prevent recalculating
    /// </summary>
    public DamageSpecifier? CurrentThresholdDamage;

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
