using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Serialization.TypeSerializers;

/// <summary>
/// A serializer wrapping the TimeOffsetSerializer that reads and writes arrays of values.
/// </summary>
public sealed class TimeOffsetArraySerializer : ITypeSerializer<TimeSpan[], SequenceDataNode>
{
    /// <inheritdoc/>
    public TimeSpan[] Read(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TimeSpan[]>? instanceProvider = null)
    {
        var list = new TimeSpan[node.Count];
        var i = 0;
        foreach (var dataNode in node)
        {
            list[i++] = serializationManager.Read<TimeSpan, ValueDataNode, TimeOffsetSerializer>((ValueDataNode)dataNode, hookCtx, context);
        }

        return list;
    }

    /// <inheritdoc/>
    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>(node.Count);
        foreach (var elem in node)
        {
            list.Add(serializationManager.ValidateNode<TimeSpan, ValueDataNode, TimeOffsetSerializer>((ValueDataNode)elem, context));
        }
        return new ValidatedSequenceNode(list);
    }

    /// <inheritdoc/>
    public DataNode Write(
        ISerializationManager serializationManager,
        TimeSpan[] value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();
        foreach (var elem in value)
        {
            sequence.Add(serializationManager.WriteValue<TimeSpan, TimeOffsetSerializer>(elem, alwaysWrite, context));
        }
        return sequence;
    }
}
