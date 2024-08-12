using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.EntityTable.EntitySelectors;

[TypeSerializer]
public sealed class EntityTableTypeSerializer :
    ITypeReader<EntityTableSelector, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.Has(EntSelector.IdDataFieldTag))
            return serializationManager.ValidateNode<EntSelector>(node, context);

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    public EntityTableSelector Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<EntityTableSelector>? instanceProvider = null)
    {
        var type = typeof(EntityTableSelector);
        if (node.Has(EntSelector.IdDataFieldTag))
            type = typeof(EntSelector);

        return (EntityTableSelector) serializationManager.Read(type, node, context)!;
    }
}
