using Content.Server.AI.HTN.PrimitiveTasks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Server.AI.HTN;

[TypeSerializer]
public sealed class HTNTaskListSerializer : ITypeSerializer<List<HTNTask>, SequenceDataNode>
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

    public List<HTNTask> Read(ISerializationManager serializationManager, SequenceDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, List<HTNTask>? value = default)
    {
        value ??= new List<HTNTask>();
        var protoManager = dependencies.Resolve<IPrototypeManager>();

        foreach (var data in node.Sequence)
        {
            var mapping = (MappingDataNode) data;

            var id = ((ValueDataNode) mapping["id"]).Value;

            if (protoManager.TryIndex<HTNCompoundTask>(id, out var compound))
            {
                value.Add(compound);
            }
            else if (protoManager.TryIndex<HTNPrimitiveTask>(id, out var primitive))
            {
                value.Add(primitive);
            }
            else
            {
                throw new InvalidOperationException($"Unable to find compound or primitive task for {id}");
            }
        }

        return value;
    }

    public DataNode Write(ISerializationManager serializationManager, List<HTNTask> value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var task in value)
        {
            var mapping = new MappingDataNode
            {
                ["id"] = new ValueDataNode(task.ID)
            };

            sequence.Add(mapping);
        }

        return sequence;
    }

    public List<HTNTask> Copy(ISerializationManager serializationManager, List<HTNTask> source, List<HTNTask> target, bool skipHook,
        ISerializationContext? context = null)
    {
        target.Clear();
        target.EnsureCapacity(source.Capacity);

        // Tasks are just prototypes soooo?
        target.AddRange(source);
        return target;
    }
}
