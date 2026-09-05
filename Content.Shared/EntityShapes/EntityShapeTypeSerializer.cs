using Content.Shared.EntityShapes.Shapes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.EntityShapes;

[TypeSerializer]
public sealed class EntityShapeTypeSerializer :
    ITypeReader<EntityShape, MappingDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.Has(NestedEntityShape.IdDataFieldTag))
            return serializationManager.ValidateNode<NestedEntityShape>(node, context);

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    public EntityShape Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<EntityShape>? instanceProvider = null)
    {
        var type = typeof(EntityShape);
        if (node.Has(NestedEntityShape.IdDataFieldTag))
            type = typeof(NestedEntityShape);

        return (EntityShape) serializationManager.Read(type, node, context)!;
    }
}
