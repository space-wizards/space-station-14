using Content.Shared.Damage;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// A need whose value decays over time. Examples include Thirst and Hunger.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class Satiation
{
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SatiationPrototype> Prototype = "";


    /// <summary>
    /// The value of this satiation as of <see cref="LastAuthoritativeChangeTime"/>.
    /// </summary>
    /// <remarks>
    /// To get the current value at any arbitrary time, use <see cref="SatiationSystem.GetValueOrNull"/>
    /// </remarks>.
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float LastAuthoritativeValue = float.MinValue;

    /// <summary>
    /// The last time <see cref="LastAuthoritativeValue"/> was modified.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastAuthoritativeChangeTime;

    /// <summary>
    /// The rate at which this satiation value is expected to decay. It is a combination of
    /// <see cref="SatiationPrototype.BaseDecayRate"/> and modifiers.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float ActualDecayRate;


    /// <summary>
    /// The current <see cref="SatiationThreshold"/>, as determined by the <see cref="SatiationSystem.GetValueOrNull">current
    /// satiation value</see>. This is stored here to avoid recalculation every time it's needed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public SatiationThreshold CurrentThreshold;

    /// <summary>
    /// <see cref="CurrentThreshold"/>'s <see cref="SatiationPrototype.ThresholdDamage"/>. This is stored here to avoid
    /// recalculation every time it's needed.
    /// </summary>
    public DamageSpecifier? CurrentThresholdDamage;


    /// <summary>
    /// When this satiation is expected to decay from <see cref="CurrentThreshold"/> to the next lower threshold. This
    /// is null when there is no lower threshold to decay to.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? ProjectedThresholdChangeTime;

    /// <summary>
    /// When continuous effects should be applied next.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextContinuousEffectTime;

    /// <summary>
    /// How often continuous effects are applied.
    /// </summary>
    [DataField]
    public TimeSpan ContinuousEffectFrequency = TimeSpan.FromSeconds(1);
}

/// <summary>
/// The broad thresholds which describe a <see cref="Satiation"/>'s state. Different thresholds cause the value to decay
/// at different rates, and for different effecets to be applied.
/// </summary>
[Serializable, NetSerializable]
public enum SatiationThreshold
{
    Dead,
    Desperate,
    Concerned,
    Okay,
    Full,
}

internal static class SatiationThresholdExtensions
{
    /// <summary>
    /// Gets the next threshold "below" this one. For example, the next threshold below
    /// <see cref="SatiationThreshold.Full">Full</see> is <see cref="SatiationThreshold.Okay">Okay</see>. Returns null
    /// if there is no lower one.
    /// </summary>
    public static SatiationThreshold? NextLower(this SatiationThreshold self)
    {
        if (self == SatiationThreshold.Dead)
            return null;
        return (SatiationThreshold)((int)self - 1);
    }
}
