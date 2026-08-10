using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// A component which is basically just a collection of <see cref="Satiation"/>s keyed by their
/// <see cref="SatiationTypePrototype"/>s.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
// Nothing should modify the dictionary once it's deserialized. Perhaps satiations can be dynamically
// added and removed in the future, but not today.
[Access(typeof(SatiationSystem))]
public sealed partial class SatiationComponent : Component
{
    /// <summary>
    /// The actual <see cref="Satiation"/>s this entity has, keyed by their <see cref="SatiationTypePrototype">type</see>.
    /// </summary>
    [DataField("satiations", required: true)]
    [AutoNetworkedField]
    private SatiationDictionary _satiations = new();

    /// <inheritdoc cref="_satiations"/>
    // Hide `SatiationDictionary` from public API
    public Dictionary<ProtoId<SatiationTypePrototype>, Satiation> Satiations => _satiations.Data;

    /// <summary>
    /// Checks if this has a <see cref="Satiation"/> of the specified <paramref name="type"/>.
    /// </summary>
    [Access(Other = AccessPermissions.Execute)]
    public bool Has(ProtoId<SatiationTypePrototype> type) => GetOrNull(type) != null;

    /// <summary>
    /// Gets the <see cref="Satiation"/> of the given <paramref name="type"/> on this component, or
    /// <c>null</c> if no such satiation exists.
    /// </summary>
    [Access(Other = AccessPermissions.Execute)]
    public Satiation? GetOrNull(ProtoId<SatiationTypePrototype> type) => Satiations.GetValueOrDefault(type);

    /// <summary>
    /// The C# code name of the backing field of <see cref="Satiations"/>, used for field deltas in
    /// <see cref="SatiationSystem"/>.
    /// </summary>
    public const string SatiationFieldName = nameof(_satiations);
}

/// <summary>
/// A specialized <c>Dictionary&lt;ProtoId&lt;SatiationTypePrototype&gt;, Satiation&gt;</c> that exists just to
/// implement <see cref="IRobustCloneable{T}"/>.
/// </summary>
[Serializable, NetSerializable, Access(typeof(SatiationDictionarySerializer))]
public sealed partial class SatiationDictionary : IRobustCloneable<SatiationDictionary>
{
    public Dictionary<ProtoId<SatiationTypePrototype>, Satiation> Data = new();

    /// <inheritdoc/>
    public SatiationDictionary Clone()
    {
        var clone = new SatiationDictionary();
        foreach (var (proto, satiation) in Data)
        {
            clone.Data[proto] = satiation.Clone();
        }

        return clone;
    }
}

[TypeSerializer]
public sealed partial class SatiationDictionarySerializer : ITypeSerializer<SatiationDictionary, MappingDataNode>
{
    private static readonly DictionarySerializer<ProtoId<SatiationTypePrototype>, Satiation> Delegate = new();

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    ) => Delegate.Validate(serializationManager, node, dependencies, context);

    public SatiationDictionary Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SatiationDictionary>? instanceProvider = null
    ) => new()
    {
        Data = Delegate.Read(
            serializationManager,
            node,
            dependencies,
            hookCtx,
            context,
            (ISerializationManager.InstantiationDelegate<Dictionary<ProtoId<SatiationTypePrototype>, Satiation>>?)null
        ),
    };

    public DataNode Write(
        ISerializationManager serializationManager,
        SatiationDictionary value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null
    ) => Delegate.Write(serializationManager, value.Data, dependencies, alwaysWrite, context);
}
