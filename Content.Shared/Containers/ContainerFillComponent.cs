using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Containers;

/// <summary>
///     Component for spawning entity prototypes into containers on map init.
/// </summary>
/// <remarks>
///     Unlike <see cref="StorageFillComponent"/> this is deterministic and supports arbitrary containers. While this
///     could maybe be merged with that component, it would require significant changes to <see
///     cref="EntitySpawnCollection.GetSpawns"/>, which is also used by several other systems.
/// </remarks>
[RegisterComponent]
public sealed partial class ContainerFillComponent : Component
{
    [DataField("containers", customTypeSerializer:typeof(ContainerFillSerializer))]
    public Dictionary<string, List<string>> Containers = new();

    /// <summary>
    ///     If true, entities spawned via the construction system will not have entities spawned into containers managed
    ///     by the construction system.
    /// </summary>
    [DataField("ignoreConstructionSpawn")]
    public bool IgnoreConstructionSpawn = true;
}

// all of this exists just to validate prototype ids.
// it would be nice if you could specify only a type validator and not have to re-implement everything else.
// or a dictionary serializer that accepts a custom type serializer for the dictionary values
public sealed class ContainerFillSerializer : ITypeValidator<Dictionary<string, List<string>>, MappingDataNode>
{
    private static PrototypeIdListSerializer<EntityPrototype> ListSerializer => new();

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var mapping = new Dictionary<ValidationNode, ValidationNode>();

        foreach (var (key, val) in node.Children)
        {
            var keyVal = serializationManager.ValidateNode<string>(key, context);

            var listVal = (val is SequenceDataNode seq)
                ? ListSerializer.Validate(serializationManager, seq, dependencies, context)
                : new ErrorNode(val, "ContainerFillComponent prototypes must be a sequence/list");

            mapping.Add(keyVal, listVal);
        }

        return new ValidatedMappingNode(mapping);
    }
}
