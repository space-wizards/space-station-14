using System.Runtime.CompilerServices;
using Content.Shared.Alert;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.Nutrition.Prototypes;

/// <summary>
/// A specific variety of satiation. For example, an animal which is always hungry would use one prototype while a
/// Diona which rarely gets hungry would use a different one.
/// </summary>
[Prototype]
public sealed partial class SatiationPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SatiationPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The base rate at which this satiation decreases per second.
    /// </summary>
    [DataField(required: true)]
    public float BaseDecayRate;

    /// <summary>
    /// The highest value this satiation can have. Increases beyond this value are clamped to this value.
    /// </summary>
    [DataField(required: true)]
    public int MaximumValue;

    /// <summary>
    /// The definition of key strings referentiable by <see cref="SatiationValue"/>s. Any reference to a key in this map
    /// by a <see cref="SatiationValue"/> will be resolved to the numeric value associated to that key here before use.
    /// <br/>
    /// Note hat different satiations can use the same keys without issue. Indeed, the intention is that a "base"
    /// satiation can define values and modifiers on itself using these keys, and then inheriting satiation prototypes
    /// can simply change the numeric values associated with those keys without needing to redefine the values and
    /// modifiers.
    /// </summary>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<string, int>))]
    public Dictionary<string, int> Thresholds = [];

    public IEnumerable<string> AllThresholdKeys => Thresholds.Keys;

    /// <summary>
    /// The lowest possible value this satiation can be initialized to.
    /// </summary>
    [DataField("startingValueMinimum", required: true)]
    private SatiationValue _startingValueMinimum = default!;

    public int StartingValueMinimum => GetValueOrNull(_startingValueMinimum) ?? 0;

    /// <summary>
    /// The highest possible value this satiation can be initialized to.
    /// </summary>
    [DataField("startingValueMaximum", required: true)]
    private SatiationValue _startingValueMaximum = default!;

    public int StartingValueMaximum => GetValueOrNull(_startingValueMaximum) ?? MaximumValue;

    /// <summary>
    /// Modifiers to <see cref="BaseDecayRate"/> based on the current threshold.
    /// </summary>
    /// <seealso cref="Satiation.ActualDecayRate"/>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<SatiationValue, float>))]
    public Dictionary<SatiationValue, float> DecayModifiers = [];

    #region Alerts

    /// <summary>
    /// The <see cref="AlertCategory"/> which all <see cref="Alerts"/> should belong to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertCategoryPrototype> AlertCategory;

    /// <summary>
    /// Alerts to show when in the corresponding threshold.
    /// </summary>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<SatiationValue, ProtoId<AlertPrototype>?>))]
    public Dictionary<SatiationValue, ProtoId<AlertPrototype>?> Alerts = [];

    /// <summary>
    /// Icons to show to accompany <see cref="Alerts"/> when in the corresponding threshold.
    /// </summary>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<SatiationValue, ProtoId<SatiationIconPrototype>?>))]
    public Dictionary<SatiationValue, ProtoId<SatiationIconPrototype>?> Icons = [];

    #endregion


    /// <summary>
    /// Clamps <paramref name="value"/> between this prototype's highest and lowest values.
    /// </summary>
    public float ClampSatiationWithinThresholds(float value) => Math.Clamp(value, 0, MaximumValue);

    /// <summary>
    /// Attempts to get an integer value from the given <paramref name="satiationValue"/>. If
    /// <paramref name="satiationValue"/> is an immediate value, use its contained integer value. If it is a key,
    /// attempts to look up the integer value of that key in <see cref="Thresholds"/>; in the case that a key not
    /// present in this proto type is requested, returns null.
    /// </summary>
    public int? GetValueOrNull(SatiationValue satiationValue)
    {
        if (satiationValue.Key is { } key)
            return Thresholds.TryGetValue(key, out var v) ? v : null;

        return satiationValue.Value;
    }
}

/// <summary>
/// This type is a union of <c>int</c> and <c>string</c> for use with satiations. When it contains an immediate integer
/// value, that value is used. When it contains a string key, that key is looked up in a
/// <see cref="SatiationPrototype"/> to resolve its integer value before use.
/// </summary>
/// <remarks>
/// Values of this type should not be created directly. Instead, rely on the implicit conversion operators. Similarly,
/// the fields in this type should not have their values modified.
/// </remarks>
/// <seealso cref="SatiationPrototype.GetValueOrNull"/>
// TODO Replace with an actual union type once that's available in C#15: https://devblogs.microsoft.com/dotnet/csharp-15-union-types/
[DataRecord, Serializable, NetSerializable]
public partial record struct SatiationValue()
{
    [DataField, Access]
    public int Value = -1;

    [DataField, Access]
    public string? Key = null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SatiationValue(int value) => new() { Value = value };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SatiationValue(string key) => new() { Key = key };
}
