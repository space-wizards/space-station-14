using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Atmos;

public sealed class GasArraySerializer : ITypeSerializer<float[], SequenceDataNode>, ITypeSerializer<float[], MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>();

        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNode<float>(elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public float[] Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<float[]>? instanceProvider = null)
    {
        var list = instanceProvider != null ? instanceProvider() : new float[Atmospherics.AdjustedNumberOfGases];

        for (var i = 0; i < node.Sequence.Count; i++)
        {
            list[i] = serializationManager.Read<float>(node.Sequence[i], hookCtx, context);
        }

        return list;
    }

    public ValidationNode Validate(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var dict = new Dictionary<ValidationNode, ValidationNode>();

        foreach (var (key, value) in node.Children)
        {
            ValidationNode keyNode = Enum.TryParse<Gas>(key, out _)
                ? new ValidatedValueNode(node.GetKeyNode(key))
                : new ErrorNode(node.GetKeyNode(key), $"Failed to parse Gas: {key}");

            dict.Add(keyNode, serializationManager.ValidateNode<float>(value, context));
        }

        return new ValidatedMappingNode(dict);
    }

    public float[] Read(ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<float[]>? instanceProvider = null)
    {
        var list = instanceProvider != null ? instanceProvider() : new float[Atmospherics.AdjustedNumberOfGases];

        foreach (var (gas, value) in node.Children)
        {
            // In the event that an invalid gas got serialized into something,
            // we simply ignore it and continue reading.
            // Errors should already be caught by Validate().
            if (!Enum.TryParse<Gas>(gas, out var gasEnum))
                continue;

            list[(int)gasEnum] = serializationManager.Read<float>(value, hookCtx, context);
        }

        return list;
    }

    public DataNode Write(ISerializationManager serializationManager,
        float[] value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var mapping = new MappingDataNode();

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            if (value[i] <= 0)
                continue;

            mapping.Add(((Gas) i).ToString(), serializationManager.WriteValue(value[i], alwaysWrite, context));
        }

        return mapping;
    }
}
