using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2.Systems;

/// <summary>
/// Defines process-scoped context for a chat message, allowing for custom data for atypical chat channels and circumstances.
/// </summary>
[Serializable, NetSerializable]
public struct ChatContext
{
    public Dictionary<string, object> Values;
}

#region Attempt Events

/// <summary>
/// Defines the abstract concept of a chat attempt.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent.</param>
[Serializable, NetSerializable]
public abstract class ChatAttemptEvent(ChatContext context, NetEntity sender, string message) : EntityEventArgs
{
    public ChatContext Context = context;
    public NetEntity Sender = sender;
    public string Message = message;
}

/// <summary>
/// Attempt a verbal chat event, specifying how loud the entity wants to be and what specific special channel they want to talk on (if any)
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
/// <param name="chatChannel">How loud the message is</param>
/// <param name="radioChannel">What specific channel to send the message on, if any</param>
[Serializable, NetSerializable]
public sealed class AttemptVerbalChatEvent(
    ChatContext context,
    NetEntity sender,
    string message,
    VerbalChatChannel chatChannel,
    ProtoId<RadioChannelPrototype>? radioChannel
) : ChatAttemptEvent(context, sender, message)
{
    public ProtoId<RadioChannelPrototype>? RadioChannel = radioChannel;
    public VerbalChatChannel ChatChannel = chatChannel;
}

/// <summary>
/// Raised when a mob tries to emote.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptVisualChatEvent(
    ChatContext context,
    NetEntity sender,
    VisualChatChannel channel,
    string message
) : ChatAttemptEvent(context, sender, message)
{
    public VisualChatChannel Channel = channel;
}

/// <summary>
/// Attempt an announcement via a communications console.
/// </summary>
/// <param name="console">The console sending the message</param>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptAnnouncementEvent(
    ChatContext context,
    NetEntity sender,
    string message,
    NetEntity console
) : ChatAttemptEvent(context, sender, message)
{
    public NetEntity Console = console;
}

/// <summary>
/// Attempt an out of character chat event, specifying how loud the entity wants to be.
/// </summary>
/// <param name="sender"></param>
/// <param name="message"></param>
/// <param name="channel"></param>
[Serializable, NetSerializable]
public sealed class AttemptOutOfCharacterChatEvent(
    ChatContext context,
    NetEntity sender,
    string message,
    OutOfCharacterChatChannel channel
) : ChatAttemptEvent(context, sender, message)
{
    public OutOfCharacterChatChannel Channel = channel;
}

#endregion

#region Failure Events

/// <summary>
/// Defines the abstract concept of chat attempts failing.
/// </summary>
[Serializable, NetSerializable]
public abstract class ChatFailedEvent(ChatContext context, NetEntity sender, string? reason) : EntityEventArgs
{
    public ChatContext Context = context;
    public NetEntity Sender = sender;
    public string? Reason = reason;
}

/// <summary>
/// Raised when a character has failed to speak.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class VerbalChatFailedEvent(ChatContext context, NetEntity sender, string? reason) : ChatFailedEvent(context, sender, reason);

/// <summary>
/// Raised when a mob has failed to emote.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class VisualChatFailedEvent(ChatContext context, NetEntity sender, string? reason) : ChatFailedEvent(context, sender, reason);

/// <summary>
/// Raised when an announcement is attempted by a communications console, and it fails for some reason.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class AnnouncementFailedEvent(ChatContext context, NetEntity sender, string? reason) : ChatFailedEvent(context, sender, reason);

public sealed class OutOfCharacterChatFailed(ChatContext context, NetEntity sender, string? reason) : ChatFailedEvent(context, sender, reason);

#endregion

#region Player-Generated Success Events

/// <summary>
/// Defines the abstract concept of succeeding at sending a chat message.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
[Serializable, NetSerializable]
public abstract class ChatSuccessEvent(ChatContext context, NetEntity speaker, string asName, string message, uint id) : EntityEventArgs
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
public sealed class VerbalChatEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    uint id,
    VerbalChatChannel chatChannel
) : ChatSuccessEvent(context, speaker, asName, message, id)
{
    public VerbalChatChannel ChatChannel = chatChannel;
}

/// <summary>
/// Raised when a mob emotes.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
[Serializable, NetSerializable]
public sealed class VisualChatEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    uint id,
    VisualChatChannel chatChannel
) : ChatSuccessEvent(context, speaker, asName, message, id)
{
    public VisualChatChannel ChatChannel = chatChannel;
}

/// <summary>
/// Raised when an announcement is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class AnnouncementEvent(
    ChatContext context,
    string asName,
    string message,
    uint id,
    Color? messageColorOverride = null
) : ChatSuccessEvent(context, NetEntity.Invalid, asName, message, id)
{
    public Color? MessageColorOverride = messageColorOverride;
}

public sealed class OutOfCharacterChatEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    uint id,
    OutOfCharacterChatChannel chatChannel
) : ChatSuccessEvent(context, speaker, asName, message, id)
{
    public OutOfCharacterChatChannel ChatChannel = chatChannel;
}

#endregion

#region Server-Generated Chat Events

/// <summary>
/// Raised when a character speaks on a radio channel.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
/// <param name="channel">The channel the message is on</param>
[Serializable, NetSerializable]
public sealed class RadioEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    ProtoId<RadioChannelPrototype> channel,
    uint id
) : ChatSuccessEvent(context, speaker, asName, message, id)
{
    public readonly ProtoId<RadioChannelPrototype> Channel = channel;
}

/// <summary>
/// Raised when a mob is given a subtle message.
/// </summary>
/// <param name="target">The target of the message</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
[Serializable, NetSerializable]
public sealed class SubtleChatEvent(NetEntity target, string message) : EntityEventArgs
{
    public NetEntity Target = target;
    public readonly string Message = message;
}

/// <summary>
/// Raised when an entity (such as a vending machine) uses local chat. The chat should not appear in the chat log.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent - although for automated messages this is unlikely.</param>
[Serializable, NetSerializable]
public sealed class BackgroundChatEvent(
    ChatContext context,
    NetEntity speaker,
    string message,
    string asName,
    VerbalChatChannel chatChannel
) : EntityEventArgs
{
    public ChatContext Context = context;
    public NetEntity Speaker = speaker;
    public string AsName = asName;
    public readonly string Message = message;
    public VerbalChatChannel ChatChannel = chatChannel;
}

#endregion
