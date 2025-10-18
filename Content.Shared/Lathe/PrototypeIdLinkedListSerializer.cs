using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Lathe;

/// <summary>
/// Handles reading, writing, and validation for linked lists of prototypes.
/// </summary>
/// <typeparam name="T">The type of prototype this linked list represents</typeparam>
/// <remarks>
/// This is in the Content.Shared.Lathe namespace as there are no other LinkedList ProtoId instances.
/// </remarks>
[TypeSerializer]
public sealed class LinkedListSerializer<T> : ITypeSerializer<LinkedList<T>, SequenceDataNode>, ITypeCopier<LinkedList<T>> where T : class
{
    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>();

        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNode<T>(elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public DataNode Write(ISerializationManager serializationManager, LinkedList<T> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var elem in value)
        {
            sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
        }

        return sequence;
    }

    LinkedList<T> ITypeReader<LinkedList<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context, ISerializationManager.InstantiationDelegate<LinkedList<T>>? instanceProvider)
    {
        var list = instanceProvider != null ? instanceProvider() : new LinkedList<T>();

        foreach (var dataNode in node.Sequence)
        {
            list.AddLast(serializationManager.Read<T>(dataNode, hookCtx, context));
        }

        return list;
    }

    public void CopyTo(
        ISerializationManager serializationManager,
        LinkedList<T> source,
        ref LinkedList<T> target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        target.Clear();
        using var enumerator = source.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            target.AddLast(current);
        }
    }
}
