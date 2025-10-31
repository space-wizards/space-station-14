using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusIcon;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using YamlDotNet.Core.Tokens;
using YamlDotNet.RepresentationModel;

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
    [DataField(required: true)]
    public float BaseDecayRate;

    /// <summary>
    /// The highest value this satiation can have. Increases beyond this value are clamped to this value.
    /// </summary>
    [DataField(required: true)]
    public int MaximumValue;

    /// <summary>
    /// The definition of key strings referentiable by <see cref="SatiationValue.SatiationValueByKey"/>. Any reference
    /// to a key in this map by a <see cref="SatiationValue.SatiationValueByKey"/> will be resolved to the numeric value
    /// associated to that key here before use.
    /// <br/>
    /// Note hat different satiations can use the same keys without issue. Indeed, the intention is that a "base"
    /// satiation can define values and modifiers on itself using these keys, and then inheriting satiation prototypes
    /// can simply change the numeric values associated with those keys without needing to redefine the values and
    /// modifiers.
    /// </summary>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<string, int>))]
    public Dictionary<string, int> Keys = [];

    public IEnumerable<string> AllThresholdKeys => Keys.Keys;

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

    /// <summary>
    /// Modifiers to movement speed based on the current threshold.
    /// </summary>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<SatiationValue, float>))]
    public Dictionary<SatiationValue, float> SpeedModifiers = [];

    /// <summary>
    /// Damage to be applied continuously based on current threshold.
    /// </summary>
    /// <seealso cref="Satiation.ContinuousEffectFrequency"/>
    /// <seealso cref="SatiationSystem.Update"/>
    [DataField(customTypeSerializer: typeof(DictionarySerializer<SatiationValue, DamageSpecifier>))]
    public Dictionary<SatiationValue, DamageSpecifier?> Damages = [];

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
    /// <paramref name="satiationValue"/> is a <see cref="SatiationValue.SatiationValueByValue"/>, use its contained integer value. If
    /// it is a <see cref="SatiationValue.SatiationValueByKey"/>, attempts to look up the integer value of that key in
    /// <see cref="Keys"/>; in the case that a key not present in this proto type is requested, returns null.
    /// </summary>
    public int? GetValueOrNull(SatiationValue satiationValue) => satiationValue switch
    {
        SatiationValue.SatiationValueByKey key => Keys.TryGetValue(key.K, out var v) ? v : null,
        SatiationValue.SatiationValueByValue value => value.V,
        _ => throw new ArgumentOutOfRangeException(nameof(satiationValue)),
    };

    public override string ToString() => $"{nameof(SatiationPrototype)}(\"{ID}\")";
}

/// <summary>
/// This type is a union of <c>int</c> and <c>string</c> for use with satiations. When it contains an immediate integer
/// value, that value is used. When it contains a string key, that key is looked up in a
/// <see cref="SatiationPrototype"/> to resolve its integer value before use.
/// </summary>
/// <seealso cref="SatiationPrototype.GetValueOrNull"/>
[ImplicitDataRecord, Serializable, NetSerializable]
public abstract record SatiationValue
{
    [DataRecord, Serializable, NetSerializable] // It's `ImplicitDataRecord`, but the game still crashed without the explicit `DataRecord` so idk
    public sealed record SatiationValueByValue(
        [field: DataField("value", required: true)]
        int V
    ) : SatiationValue;

    [DataRecord, Serializable, NetSerializable]
    public sealed record SatiationValueByKey(
        [field: DataField("key", required: true)]
        string K
    ) : SatiationValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SatiationValue(int value) => new SatiationValueByValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SatiationValue(string key) => new SatiationValueByKey(key);

    [UsedImplicitly, TypeSerializer]
    public sealed class SatiationValueSerializer : ITypeReader<SatiationValue, ValueDataNode>
    {
        public ValidationNode Validate(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null
        )
        {
            return int.TryParse(node.Value, out _)
                ? serializationManager.ValidateNode<int>(node, context)
                : serializationManager.ValidateNode<string>(node, context);
        }

        public SatiationValue Read(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<SatiationValue>? instanceProvider = null
        )
        {
            if (int.TryParse(node.Value, out _))
                return new SatiationValueByValue(serializationManager.Read<int>(node, context));

            return new SatiationValueByKey(serializationManager.Read<string>(node, context, notNullableOverride: true));
        }
    }
}
