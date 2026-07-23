using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Cards;

public sealed class CardDataSerializer : ITypeSerializer<List<CardData>, SequenceDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>();

        foreach (var elem in node.Sequence)
            list.Add(serializationManager.ValidateNode<ProtoId<CardPrototype>>(elem, context));

        return new ValidatedSequenceNode(list);
    }

    public List<CardData> Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<List<CardData>>? instanceProvider = null)
    {
        var list = instanceProvider != null ? instanceProvider() : new List<CardData>();

        foreach (var elem in node.Sequence)
        {
            var protoId = serializationManager.Read<ProtoId<CardPrototype>>(elem, hookCtx, context);
            list.Add(new CardData(protoId));
        }

        return list;
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        List<CardData> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var card in value)
            sequence.Add(serializationManager.WriteValue(card.CardId, alwaysWrite, context));

        return sequence;
    }
}
