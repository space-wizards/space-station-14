using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Chat.Prototypes;

[Serializable]
[Prototype("communicationChannel")]
public sealed partial class CommunicationChannelPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The prototype we inherit from.
    /// </summary>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CommunicationChannelPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// The conditions required for a session (i.e. client) to be allowed to publish via this communication channel.
    /// </summary>
    [DataField(serverOnly: true)]
    [AlwaysPushInheritance]
    public List<IChatCondition> PublishChatConditions = new();

    /// <summary>
    /// The conditions required for a session (i.e. client) to be allowed to consume a message from this communication channel.
    /// Clients/entities prioritize the ConsumeCollections in order; if a consumer is a'ctive in a collection it will not be included in any subsequent collections in the list.
    /// </summary>
    [DataField(serverOnly: true)]
    [AlwaysPushInheritance]
    public List<IChatCondition> ConsumeChatConditions = new();

    /// <summary>
    /// Contains modifiers that are applied on the server.
    /// Avoid including text nodes or direct text changes/formatting unless necessary for security reasons.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ChatModifier> ServerModifiers = new();

    /// <summary>
    /// A collection of conditions and applicable modifiers.
    /// If a condition is met, the corresponding modifier(s) will be applied to the message AFTER the ServerModifiers.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ConsumeCollection> ConditionalModifiers = new();

    /// <summary>
    /// Contains modifiers that are applied and processed on the client.
    /// Clientsided ChatModifiers may provide text nodes and include text formatting;
    /// because of this, attention should be paid to the list order to ensure the correct order of operations.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ChatModifier> ClientModifiers = new();

    /// <summary>
    /// The kind of chat filter this channel works under; used to filter chat clientside.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ChatChannelFilter ChatFilter = ChatChannelFilter.None;

    /// <summary>
    /// The way the message is conveyed in the game; audio, visual, OOC or such.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ChatChannelMedium ChatMedium = ChatChannelMedium.None;

    /// <summary>
    /// If true, the same message may not be published twice on the same channel.
    /// Useful to be true for radio channels but false for whispers, to allow relaying via intercoms without looping.
    /// EXTREME ATTENTION TO AVOID LOOPING must be paid if this value is set to false, as improper message relay can cause an infinite loop.
    /// If someone knows how to make a good check to avoid that, please implement it!
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool NonRepeatable = true;

    /// <summary>
    /// If set, a message published on the current channel will also try to publish to these communication channels.
    /// Channels are evaluated separately.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ProtoId<CommunicationChannelPrototype>>? AlwaysChildCommunicationChannels;

    /// <summary>
    /// If set, a message that fails the conditions to publish on the current channel will try to publish to these communication channels instead.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<ProtoId<CommunicationChannelPrototype>>? BackupChildCommunicationChannels;

    /// <summary>
    /// If true, any message published to this channel won't show up in the chatbox.
    /// Useful for vending machines, bots and other speech bubble pop-ups.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public bool HideChat = false;

    /// <summary>
    /// Used as input for certain conditions and node suppliers, for when they need more customizability than what is defined in the channel prototype.
    /// An example of this would be overriding ColorFulltextMarkupChatModifier's color key when doing communication console alert levels, or defining radio channels.
    /// The parameters are *not* communicated between server and client, but may be shared via the prototype.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<Enum, object> ChannelParameters = new();
}

[Serializable]
[DataDefinition]
public partial struct ConsumeCollection
{
    /// <summary>
    /// The conditions required for a consumer to be allowed to consume this communication channel.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<IChatCondition> Conditions = new();

    /// <summary>
    /// Contains markup node suppliers that are applied and processed on the server.
    /// Serversided node suppliers should avoid adding text nodes or formatting tags where possible.
    /// If such nodes are needed, it's better to supply a tag that gets converted appropriately on the client instead.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<ChatModifier> Modifiers = new();

    public ConsumeCollection(
        List<IChatCondition> conditions,
        List<ChatModifier> modifiers)
    {
        Conditions = conditions;
        Modifiers = modifiers;
    }
}

/// <summary>
/// Default channel parameters utilized by various conditions and suppliers. Not guaranteed to be set.
/// </summary>
[Serializable]
public enum DefaultChannelParameters
{
    SenderEntity,
    SenderSession,
    RandomSeed,
    RadioChannel,
    GlobalAudioPath,
    GlobalAudioVolume,
}
