using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.Prototypes;

[Prototype("communicationChannel")]
public sealed partial class CommunicationChannelPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;

    /// <summary>
    /// The conditions required for a session (i.e. client) to be allowed to publish via this communication channel.
    /// </summary>
    [DataField]
    public List<SessionChatCondition> PublishSessionChatConditions = new();

    /// <summary>
    /// The conditions required for an entity (non-client) to be allowed to publish via this communication channel.
    /// </summary>
    [DataField]
    public List<EntityChatCondition> PublishEntityChatConditions = new();

    /// <summary>
    /// A collection of consumer conditions and applicable markup tags.
    /// Clients/entities prioritize the ConsumeCollections in order; if a consumer is active in a collection it will not be included in any subsequent collections in the list.
    /// </summary>
    [DataField]
    public List<ConsumeCollection> ConsumeCollections = new();

    /// <summary>
    /// The kind of communication types this channel utilizes (e.g. speech, OOC, etc.)
    /// </summary>
    [DataField]
    public List<ProtoId<CommunicationTypePrototype>> CommunicationTypes = new();

    /// <summary>
    /// If true, the server may publish to this channel.
    /// </summary>
    [DataField]
    public bool AllowServerMessages = true;

    /// <summary>
    /// If true, an entity does not need to be attached to publish to this channel.
    /// </summary>
    [DataField]
    public bool AllowEntitylessMessages = true;

    /// <summary>
    /// If true, the same message may not be published twice on the same channel.
    /// Useful to be true for radio channels but false for whispers, to allow relaying via intercoms without looping.
    /// EXTREME ATTENTION TO AVOID LOOPING must be paid if this value is set to false, as improper message relay can cause an infinite loop.
    /// If someone knows how to make a good check to avoid that, please implement it!
    /// </summary>
    [DataField]
    public bool NonRepeatable = true;

    /// <summary>
    /// If set, a message published on this channel will also try to publish to these communication channels.
    /// Channels are evaluated separately.
    /// </summary>
    [DataField]
    public List<ProtoId<CommunicationChannelPrototype>>? ChildCommunicationChannels;

    [DataField]
    public List<MarkupNodeSupplier> ChannelMarkupNodes = new();
}

[Serializable]
[DataDefinition]
public partial struct ConsumeCollection
{
    /// <summary>
    /// The conditions required for a session (i.e. client) to be allowed to consume this communication channel.
    /// </summary>
    [DataField]
    public List<SessionChatCondition> ConsumeSessionChatConditions = new();

    /// <summary>
    /// The conditions required for an entity (non-client) to be allowed to consume this communication channel.
    /// </summary>
    [DataField]
    public List<EntityChatCondition> ConsumeEntityChatConditions = new();

    /// <summary>
    /// The markup nodes that should be supplied to this collection of consumers.
    /// </summary>
    [DataField]
    public List<MarkupNodeSupplier> CollectionMarkupNodes = new();

    public ConsumeCollection(
        List<SessionChatCondition> consumeSessionChatConditions,
        List<EntityChatCondition> consumeEntityChatConditions,
        List<MarkupNodeSupplier> collectionMarkupNodes)
    {
        ConsumeSessionChatConditions = consumeSessionChatConditions;
        ConsumeEntityChatConditions = consumeEntityChatConditions;
        CollectionMarkupNodes = collectionMarkupNodes;
    }
}
