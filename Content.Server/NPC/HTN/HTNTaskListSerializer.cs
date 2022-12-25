using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Server.NPC.HTN;

public sealed class HTNTaskListSerializer : ITypeSerializer<List<string>, SequenceDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>();
        var protoManager = dependencies.Resolve<IPrototypeManager>();

        foreach (var data in node.Sequence)
        {
            if (data is not MappingDataNode mapping)
            {
                list.Add(new ErrorNode(data, $"Found invalid mapping node on {data}"));
                continue;
            }

            var id = ((ValueDataNode) mapping["id"]).Value;

            var isCompound = protoManager.HasIndex<HTNCompoundTask>(id);
            var isPrimitive = protoManager.HasIndex<HTNPrimitiveTask>(id);

            list.Add(isCompound ^ isPrimitive
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"Found duplicated HTN compound and primitive tasks for {id}"));
        }

        return new ValidatedSequenceNode(list);
    }

    public List<string> Read(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<List<string>>? instanceProvider = null)
    {
        var value = instanceProvider != null ? instanceProvider() : new List<string>();
        foreach (var data in node.Sequence)
        {
            var mapping = (MappingDataNode) data;
            var id = ((ValueDataNode) mapping["id"]).Value;
            // Can't check prototypes here because we're still loading them so yay!
            value.Add(id);
        }

        return value;
    }

    public DataNode Write(ISerializationManager serializationManager, List<string> value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var task in value)
        {
            var mapping = new MappingDataNode
            {
                ["id"] = new ValueDataNode(task)
            };

            sequence.Add(mapping);
        }

        return sequence;
    }
}
