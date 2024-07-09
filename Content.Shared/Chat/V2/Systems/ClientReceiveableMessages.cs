using Content.Shared.Chat.V2.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2.Systems;

/// <summary>
/// Defines the abstract concept of succeeding at sending a chat message.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
[Serializable, NetSerializable]
public abstract class ChatReceivedEvent(ChatContext context, NetEntity speaker, string asName, string message, uint id) : EntityEventArgs
{
    public ChatContext Context = context;
    public NetEntity Speaker = speaker;
    public string AsName = asName;
    public readonly string Message = message;
    public uint Id = id;
}

/// <summary>
/// Raised to inform clients that an entity has spoken.
/// </summary>
[Serializable, NetSerializable]
public sealed class VerbalChatReceivedEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    uint id,
    ProtoId<VerbalChatChannelPrototype> chatChannel
) : ChatReceivedEvent(context, speaker, asName, message, id)
{
    public ProtoId<VerbalChatChannelPrototype> ChatChannel = chatChannel;
}

/// <summary>
/// Raised when a mob emotes.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
[Serializable, NetSerializable]
public sealed class VisualChatRecievedEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    uint id,
    ProtoId<VisualChatChannelPrototype>  chatChannel
) : ChatReceivedEvent(context, speaker, asName, message, id)
{
    public ProtoId<VisualChatChannelPrototype> ChatChannel = chatChannel;
}

/// <summary>
/// Raised when an announcement is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class AnnouncementReceivedEvent(
    ChatContext context,
    string asName,
    string message,
    uint id,
    Color? messageColorOverride = null
) : ChatReceivedEvent(context, NetEntity.Invalid, asName, message, id)
{
    public Color? MessageColorOverride = messageColorOverride;
}

public sealed class OutOfCharacterChatReceivedEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    uint id,
    ProtoId<OutOfCharacterChannelPrototype>  chatChannel
) : ChatReceivedEvent(context, speaker, asName, message, id)
{
    public ProtoId<OutOfCharacterChannelPrototype>  ChatChannel = chatChannel;
}

/// <summary>
/// Raised when a character speaks on a radio channel.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
/// <param name="channel">The channel the message is on</param>
[Serializable, NetSerializable]
public sealed class RadioReceivedEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    ProtoId<RadioChannelPrototype> channel,
    uint id
) : ChatReceivedEvent(context, speaker, asName, message, id)
{
    public readonly ProtoId<RadioChannelPrototype> Channel = channel;
}

/// <summary>
/// Raised when a mob is given a subtle message.
/// </summary>
/// <param name="target">The target of the message</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
[Serializable, NetSerializable]
public sealed class SubtleChatReceivedEvent(NetEntity target, string message) : EntityEventArgs
{
    public NetEntity Target = target;
    public readonly string Message = message;
}
