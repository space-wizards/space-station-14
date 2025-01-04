using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Utility;
using YamlDotNet.Core.Tokens;

namespace Content.Shared.Nutrition.Prototypes;

/// <summary>
/// A specific variety of satiation. For example, an animal which is always hungry would use one prototype while a
/// Diona which rarely gets hungry would use a different one.
/// </summary>
[Prototype]
public sealed class SatiationPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SatiationPrototype>))]
    public string[]? Parents { get; }

    /// <inheritdoc/>
    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// The base rate at which this satiation decreases per second.
    /// </summary>
    [DataField]
    public float BaseDecayRate;

    /// <summary>
    /// The values which define the boundaries of <see cref="SatiationThreshold"/> for this prototype.
    /// </summary>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<SatiationThreshold, float>))]
    public Dictionary<SatiationThreshold, float> Thresholds = [];

    /// <summary>
    /// Modifiers to <see cref="BaseDecayRate"/> based on the current threshold.
    /// </summary>
    /// <seealso cref="Satiation.ActualDecayRate"/>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<SatiationThreshold, float>))]
    public Dictionary<SatiationThreshold, float> ThresholdDecayModifiers = [];

    /// <summary>
    /// A modifier applied to the owner's movement speed when this satiation is at or below <see cref="SatiationThreshold.Concerned"/>.
    /// </summary>
    [DataField]
    public float SlowdownModifier;

    /// <summary>
    /// Damage to be applied continuously based on current threshold.
    /// </summary>
    /// <seealso cref="Satiation.ContinuousEffectFrequency"/>
    /// <seealso cref="SatiationSystem.Update"/>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<SatiationThreshold, DamageSpecifier>))]
    public Dictionary<SatiationThreshold, DamageSpecifier> ThresholdDamage = [];

    #region Alerts

    /// <summary>
    /// The <see cref="AlertCategory"/> which all <see cref="Alerts"/> should belong to.
    /// </summary>
    [DataField]
    public ProtoId<AlertCategoryPrototype> AlertCategory = default!;

    /// <summary>
    /// Alerts to show when in the corresponding threshold.
    /// </summary>
    [DataField]
    public Dictionary<SatiationThreshold, ProtoId<AlertPrototype>> Alerts = [];

    /// <summary>
    /// Icons to show to accompany <see cref="Alerts"/> when in the corresponding threshold.
    /// </summary>
    [DataField]
    public Dictionary<SatiationThreshold, ProtoId<SatiationIconPrototype>> Icons = [];

    #endregion


    #region helpers

    /// <summary>
    /// Calculates the <see cref="SatiationThreshold"/> corresponding to the given <paramref name="value"/> for this
    /// prototype.
    /// </summary>
    public SatiationThreshold ThresholdFor(float value) =>
        Thresholds
            .Reverse()
            .FirstOrNull(threshold => value <= threshold.Value)
            ?.Key
        ?? SatiationThreshold.Dead;

    /// <summary>
    /// Clamps <paramref name="value"/> between this prototype's <see cref="SatiationThreshold.Dead"/> and
    /// <see cref="SatiationThreshold.Full"/> values.
    /// </summary>
    public float ClampSatiationWithinThresholds(float value) => Math.Clamp(value,
        Thresholds[SatiationThreshold.Dead],
        Thresholds[SatiationThreshold.Full]);

    #endregion
}
