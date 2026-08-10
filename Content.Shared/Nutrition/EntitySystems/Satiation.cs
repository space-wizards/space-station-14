using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
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
public sealed partial class Satiation
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
    [IncludeDataField]
    public Dictionary<SatiationValue, T> Thresholds = [];

    /// <summary>
    /// When this satiation is expected to change from its current threshold to a different one. This is null when the
    /// current linear change is zero or there is no threshold in the direction of the expected change.
    /// </summary>
    [ViewVariables]
    public TimeSpan? ProjectedThresholdChangeTime = TimeSpan.Zero; // Initialize to zero to force an update as soon as possible on load

    /// <summary>
    /// The current <typeparamref name="T"/> value, at least when maintained by something like
    /// <see cref="BaseSatiationEffectSystem{TComp,T}"/>
    /// </summary>
    [ViewVariables]
    public T Current;
}

[TypeSerializer]
public sealed partial class SatiationThresholdsSerializer<T> : ITypeSerializer<SatiationThresholds<T>, MappingDataNode>,
    ITypeCopier<SatiationThresholds<T>>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    ) => serializationManager.ValidateNode<Dictionary<SatiationValue, T>>(node, context);

    public SatiationThresholds<T> Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SatiationThresholds<T>>? instanceProvider = null
    ) => new()
    {
        Thresholds = serializationManager.Read<Dictionary<SatiationValue, T>>(
            node,
            context,
            hookCtx.SkipHooks,
            instanceProvider is { } ip ? () => ip().Thresholds : null,
            true
        ),
    };

    public DataNode Write(
        ISerializationManager serializationManager,
        SatiationThresholds<T> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
    ) => serializationManager.WriteValue(value.Thresholds, alwaysWrite, context, true);

    public void CopyTo(
        ISerializationManager serializationManager,
        SatiationThresholds<T> source,
        ref SatiationThresholds<T> target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null
    ) => serializationManager.CopyTo(source.Thresholds, ref target.Thresholds, context, true, true);
}
