using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.SmartFridge;

[TypeSerializer]
public sealed class SmartFridgeEntrySerializer :
    ITypeSerializer<SmartFridgeEntry, ValueDataNode>,
    ITypeReader<SmartFridgeEntry, MappingDataNode> // For backwards compatibility with the DataRecord serializer
{
    public SmartFridgeEntry Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SmartFridgeEntry>? instanceProvider = null)
    {
        return new SmartFridgeEntry(node.Value);
    }

    public DataNode Write(ISerializationManager serializationManager,
        SmartFridgeEntry value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.Name);
    }

    public ValidationNode Validate(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return new ValidatedValueNode(node);
    }


    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.TryGetValue("name", out var valNode) && valNode is ValueDataNode)
            return new ValidatedValueNode(valNode);

        return new ErrorNode(node, "must contain name entry");
    }

    public SmartFridgeEntry Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SmartFridgeEntry>? instanceProvider = null)
    {
        var valNode = (ValueDataNode)node["name"];
        return new  SmartFridgeEntry(valNode.Value);
    }
}
