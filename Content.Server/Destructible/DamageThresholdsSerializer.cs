#nullable disable
using System.Linq;
using Content.Server.Destructible.Thresholds;
using Content.Shared.Destructible.Thresholds.Triggers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Server.Destructible;

public sealed class DamageThresholdsSerializer : ITypeSerializer<List<DamageThreshold>, SequenceDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext context = null)
    {
        var list = new List<ValidationNode>();
        foreach (var elem in node.Sequence)
        {
            list.Add(serializationManager.ValidateNode<DamageThreshold>(elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public List<DamageThreshold> Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext context = null,
        ISerializationManager.InstantiationDelegate<List<DamageThreshold>> instanceProvider = null)
    {
        if (instanceProvider != null)
        {
            var sawmill = dependencies.Resolve<ILogManager>().GetSawmill("szr");
            sawmill.Warning($"Provided value to a Read-call for a {nameof(List<DamageThreshold>)}. Ignoring...");
        }

        var list = new List<DamageThreshold>();

        var ignore = new HashSet<int>(node.Sequence.Count);
        for (var i = 0; i < node.Sequence.Count; i++)
        {
            if (ignore.Contains(i))
                continue;

            var item = serializationManager.Read<DamageThreshold>(node.Sequence[i], hookCtx, context);
            for (var j = i + 1; j < node.Sequence.Count; j++)
            {
                var item2 = serializationManager.Read<DamageThreshold>(node.Sequence[j], hookCtx, context);
                if (IsEqual(item, item2))
                {
                    item.Behaviors = item.Behaviors.Union(item2.Behaviors).ToList();
                    ignore.Add(j);
                }
            }

            list.Add(item);
        }

        list.Sort();
        return list;
    }

    public DataNode Write(ISerializationManager serializationManager,
        List<DamageThreshold> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var elem in value)
        {
            sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
        }

        return sequence;
    }

    private bool IsEqual(DamageThreshold a, DamageThreshold b)
    {
        return IsEqual(a.Trigger, b.Trigger);
    }

    private bool IsEqual<T1, T2>(T1 a, T2 b) where T1 : IThresholdTrigger where T2 : IThresholdTrigger
    {
        // Ensure same type and same thresholds!
        return a is T2 trigger && trigger.Equals(b);
    }
}
