using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Chemistry.Reagent;

[TypeSerializer]
public sealed class ReagentMetabolismsSerializer : ITypeInheritanceHandler<ReagentMetabolisms, MappingDataNode>
{
    public MappingDataNode PushInheritance(
        ISerializationManager serializationManager,
        MappingDataNode child,
        MappingDataNode parent,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        var result = child.Copy();

        foreach (var (k, v) in parent)
        {
            if (result.TryAddCopy(k, v))
                continue;

            result[k] = serializationManager.CombineMappings(
                (MappingDataNode)result[k],
                (MappingDataNode)v);
        }

        return result;
    }
}
