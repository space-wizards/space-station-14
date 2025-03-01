using Content.Shared.Chat.Prototypes;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory;
using Robust.Shared.Utility;

namespace Content.Shared.Chat;

/// <summary>
///     This event should be sent everytime an entity talks (Radio, local chat, etc...).
///     The event is sent to both the entity itself, and all clothing (For stuff like voice masks).
/// </summary>
public sealed class TransformSpeakerNameEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public EntityUid Sender;
    public string VoiceName;
    public ProtoId<SpeechVerbPrototype>? SpeechVerb;

    public TransformSpeakerNameEvent(EntityUid sender, string name)
    {
        Sender = sender;
        VoiceName = name;
        SpeechVerb = null;
    }
}

/// <summary>
/// Event raised on all entities that are allowed to consume this message and communication type.
/// Should NOT be subscribed to directly, as it also handles separating component functionality based on communication type.
/// Make your system inherit ListenerEntitySystem instead.
/// </summary>
public sealed class ListenerConsumeEvent : EntityEventArgs
{
    public ChatChannelMedium ChatMedium;

    public FormattedMessage Message;

    public Dictionary<Enum, object> ChannelParameters;

    public ListenerConsumeEvent(ChatChannelMedium chatMedium, FormattedMessage message, Dictionary<Enum, object> channelParameters)
    {
        ChatMedium = chatMedium;
        Message = message;
        ChannelParameters = channelParameters;
    }
}

/// <summary>
/// Gets a hashset of all the entities that have a component deriving from ListenerComponent.
/// </summary>
[ByRefEvent]
public record struct GetListenerConsumersEvent(HashSet<EntityUid> Entities);
