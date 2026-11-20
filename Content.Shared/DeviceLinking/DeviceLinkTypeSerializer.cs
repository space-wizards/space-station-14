using System.Linq;
using Content.Shared.DeviceLinking.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// A custom type serializer for <see cref="DeviceLinkSourceComponent"/> that converts
/// the old Tuple format into a more readable <see cref="DeviceLink"/> struct.
/// </summary>
// TODO FUTURE this is a migrations serializer, meaning it should be removed entirely once all maps are re-saved.
public sealed class DeviceLinkTypeSerializer : ITypeSerializer<Dictionary<EntityUid, HashSet<DeviceLink>>, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var newFormat = serializationManager.ValidateNode<Dictionary<EntityUid, HashSet<DeviceLink>>>(node);
        return newFormat is not ErrorNode
            ? newFormat
            : serializationManager.ValidateNode<Dictionary<EntityUid, HashSet<(ProtoId<SourcePortPrototype>, ProtoId<SinkPortPrototype>)>>>(node);
    }

    public Dictionary<EntityUid, HashSet<DeviceLink>> Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Dictionary<EntityUid, HashSet<DeviceLink>>>? instanceProvider = null)
    {
        // Read the dictionary
        var dict = instanceProvider != null ? instanceProvider() : new Dictionary<EntityUid, HashSet<DeviceLink>>(node.Children.Count);

        var keyNode = new ValueDataNode();
        foreach (var (key, value) in node.Children)
        {
            keyNode.Value = key;
            HashSet<DeviceLink> setValue;
            if (serializationManager.ValidateNode<HashSet<DeviceLink>>(value) is ValidatedSequenceNode { Valid: true })
            {
                setValue = serializationManager.Read<HashSet<DeviceLink>>(value, context, notNullableOverride: true);
            }
            else
            {
                var cache = serializationManager.Read<HashSet<(ProtoId<SourcePortPrototype>, ProtoId<SinkPortPrototype>)>>(value, context, notNullableOverride: true);
                setValue = cache.Select(x => new DeviceLink(x.Item1, x.Item2)).ToHashSet();
            }

            dict.Add(serializationManager.Read<EntityUid>(keyNode, hookCtx, context), setValue);
        }

        return dict;
    }

    public DataNode Write(ISerializationManager serializationManager,
        Dictionary<EntityUid, HashSet<DeviceLink>> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        // Always writes in a struct format
        return serializationManager.WriteValue(value, notNullableOverride: true, context: context);
    }
}
