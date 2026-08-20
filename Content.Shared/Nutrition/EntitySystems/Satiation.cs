using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// A need whose value changes over time. Examples include Thirst and Hunger.
/// </summary>
/// <remarks>
/// While public, this type should not be used in <see cref="SatiationSystem"/> API methods. Instead, pass
/// <see cref="SatiationComponent"/> and a <see cref="SatiationTypePrototype"/> (or its <see cref="ProtoId{T}"/>).
/// This is to allow people unfamiliar with the internals of satiation to work with a component and a prototype,
/// concepts which should be familiar to anyone working in Robust C#.
/// </remarks>
[DataDefinition, Serializable, NetSerializable, Access(typeof(SatiationSystem))]
public sealed partial class Satiation : IRobustCloneable<Satiation>
{
    /// <summary>
    /// This satiation's <see cref="SatiationTypePrototype"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SatiationTypePrototype> SatiationType;

    /// <summary>
    /// This satiation's <see cref="SatiationPrototype"/>, which describes how it changes over time.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SatiationPrototype> Prototype;

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
    /// The rate at which this satiation value is expected to change. It is a combination of
    /// <see cref="SatiationPrototype.BaseChangeRate"/> and modifiers.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float ActualChangeRate;

    /// <summary>
    /// When <see cref="ActualChangeRate"/> is expected to change, if nothing but time affects this satiation. This is
    /// used to predict satiation updates on clients.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextChangeRateModUpdateTime;

    /// <summary>
    /// <see cref="NextChangeRateModUpdateTime"/>, but for satiation alerts.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextAlertUpdateTime;

    /// <inheritdoc/>
    public Satiation Clone() => new()
    {
        SatiationType = SatiationType,
        Prototype = Prototype,
        LastAuthoritativeValue = LastAuthoritativeValue,
        LastAuthoritativeChangeTime = LastAuthoritativeChangeTime,
        ActualChangeRate = ActualChangeRate,
        NextChangeRateModUpdateTime = NextChangeRateModUpdateTime,
        NextAlertUpdateTime = NextAlertUpdateTime,
    };
}

/// <summary>
/// A combination of configuration (<see cref="Thresholds"/>) and state (<see cref="Current"/>) which describes how a
/// <typeparamref name="T"/> value changes related to <see cref="Satiation"/>, as well as its current value.
/// </summary>
/// <example>
/// <see cref="SatiationDamageSystem"/> uses this to describe and track damage descriptors based on satiations, applying
/// the damage regularly over time. <see cref="Thresholds"/> describes the damage to apply at various levels of
/// satiation, and <see cref="Current"/> tracks the current damage to apply every update, meaning it doesn't need to be
/// looked up from <see cref="Thresholds"/> on every tick.
/// </example>
/// <remarks>This should probably only ever be used in conjunction with <see cref="BaseSatiationEffectSystem{TComp,T}"/></remarks>
[DataDefinition, Serializable]
public sealed partial class SatiationThresholds<T>
{
    /// <summary>
    /// The <typeparamref name="T"/> values keyed by the satiation values at or below which the T value becomes "active".
    /// </summary>
    /// <seealso cref="SatiationSystem.TryGetValueByThreshold"/>
    [DataField(ThresholdsTag)]
    public Dictionary<SatiationValue, T> Thresholds = [];

    /// <summary>
    /// When this satiation is expected to change from its current threshold to a different one. This is null when the
    /// current linear change is zero or there is no threshold in the direction of the expected change.
    /// </summary>
    [DataField(ProjectedThresholdChangeTimeTag, customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? ProjectedThresholdChangeTime;

    /// <summary>
    /// The current <typeparamref name="T"/> value, at least when maintained by something like
    /// <see cref="BaseSatiationEffectSystem{TComp,T}"/>
    /// </summary>
    [DataField(CurrentTag), ViewVariables]
    public T Current;

    public const string ThresholdsTag = "thresholds";
    public const string ProjectedThresholdChangeTimeTag = "projectedThresholdChangeTime";
    public const string CurrentTag = "current";
}

/// <summary>
/// The serializer for <see cref="SatiationThresholds{T}"/>. Manually implemented because scary generic <c>T</c>.
/// </summary>
[TypeSerializer]
public sealed partial class SatiationThresholdsSerializer<T> :
    ITypeSerializer<SatiationThresholds<T>, MappingDataNode>,
    ITypeCopyCreator<SatiationThresholds<T>>
{
    /// <inheritdoc/>
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    )
    {
        var ret = new Dictionary<ValidationNode, ValidationNode>();

        if (node.TryGetValue(SatiationThresholds<T>.ThresholdsTag, out var thresholds))
        {
            ret[new ValidatedValueNode(node.GetKeyNode(SatiationThresholds<T>.ThresholdsTag))] =
                serializationManager.ValidateNode<Dictionary<SatiationValue, T>>(thresholds, context);
        }

        if (node.TryGetValue(SatiationThresholds<T>.ProjectedThresholdChangeTimeTag, out var changeTime))
        {
            ret[new ValidatedValueNode(node.GetKeyNode(SatiationThresholds<T>.ProjectedThresholdChangeTimeTag))] =
                changeTime is ValueDataNode v
                    ? serializationManager.ValidateNode<TimeSpan, ValueDataNode, TimeOffsetSerializer>(v, context)
                    : new ErrorNode(changeTime, $"Expected {typeof(ValueDataNode)}, got {changeTime.GetType()}");
        }

        if (node.TryGetValue(SatiationThresholds<T>.CurrentTag, out var current))
        {
            ret[new ValidatedValueNode(node.GetKeyNode(SatiationThresholds<T>.CurrentTag))] =
                serializationManager.ValidateNode<T>(current, context);
        }

        return new ValidatedMappingNode(ret);
    }

    /// <inheritdoc/>
    public SatiationThresholds<T> Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SatiationThresholds<T>>? instanceProvider = null
    ) => new()
    {
        Thresholds = node.TryGetValue(SatiationThresholds<T>.ThresholdsTag, out var thresholdsNode)
            ? serializationManager.Read<Dictionary<SatiationValue, T>>(
                thresholdsNode,
                context,
                notNullableOverride: true
            )
            : [],
        ProjectedThresholdChangeTime =
            node.TryGetValue(SatiationThresholds<T>.ProjectedThresholdChangeTimeTag, out var changeTime)
                ? serializationManager.Read<TimeSpan, ValueDataNode, TimeOffsetSerializer>(
                    (ValueDataNode)changeTime,
                    hookCtx,
                    context
                )
                : null,
        Current = node.TryGetValue(SatiationThresholds<T>.CurrentTag, out var currentNode)
            ? serializationManager.Read<T>(currentNode, hookCtx, context, null, true)
            : default!,
    };

    public SatiationThresholds<T> CreateCopy(
        ISerializationManager serializationManager,
        SatiationThresholds<T> source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null
    ) => new()
    {
        Thresholds = serializationManager.CreateCopy(source.Thresholds, hookCtx, context),
        ProjectedThresholdChangeTime = source.ProjectedThresholdChangeTime is { } changeTime
            ? serializationManager.CreateCopy<TimeSpan, TimeOffsetSerializer>(changeTime, hookCtx, context)
            : null,
        Current = serializationManager.CreateCopy(source.Current, hookCtx, context),
    };

    /// <inheritdoc/>
    public DataNode Write(
        ISerializationManager serializationManager,
        SatiationThresholds<T> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
    )
    {
        var ret = new Dictionary<string, DataNode>
        {
            [SatiationThresholds<T>.ThresholdsTag] =
                serializationManager.WriteValue(value.Thresholds, alwaysWrite, context, true),
        };

        if (value.ProjectedThresholdChangeTime is { } changeTime)
        {
            ret[SatiationThresholds<T>.ProjectedThresholdChangeTimeTag] = serializationManager.WriteValue<
                TimeSpan,
                TimeOffsetSerializer>(
                changeTime,
                alwaysWrite,
                context
            );
        }

        if (alwaysWrite || !EqualityComparer<T>.Default.Equals(value.Current, default!))
        {
            ret[SatiationThresholds<T>.CurrentTag] =
                serializationManager.WriteValue(value.Current, alwaysWrite, context, true);
        }

        return new MappingDataNode(ret);
    }
}
