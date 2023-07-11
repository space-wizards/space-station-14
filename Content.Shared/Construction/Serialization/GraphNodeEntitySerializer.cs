using Content.Shared.Construction.NodeEntities;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Construction.Serialization;

public sealed class GraphNodeEntitySerializer : ITypeSerializer<IGraphNodeEntity, ValueDataNode>, ITypeSerializer<IGraphNodeEntity, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var id = node.Value;

        var protoMan = dependencies.Resolve<IPrototypeManager>();

        if (!protoMan.HasIndex<EntityPrototype>(id))
        {
            return new ErrorNode(node, $"Entity Prototype {id} was not found!");
        }

        return new ValidatedValueNode(node);
    }

    public IGraphNodeEntity Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<IGraphNodeEntity>? instanceProvider = null)
    {
        return new StaticNodeEntity(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<IGraphNodeEntity>(node, context);
    }

    public IGraphNodeEntity Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<IGraphNodeEntity>? instanceProvider = null)
    {
        return serializationManager.Read(node, hookCtx, context, instanceProvider, false);
    }

    public DataNode Write(ISerializationManager serializationManager, IGraphNodeEntity value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value, alwaysWrite, context, false);
    }
}
