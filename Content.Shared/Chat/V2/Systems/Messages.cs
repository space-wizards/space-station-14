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
public abstract class ChatAttemptEvent(NetEntity sender, string message) : EntityEventArgs
{
    public ChatContext Context;
    public NetEntity Sender = sender;
    public string Message = message;
}

/// <summary>
/// Attempt a verbal chat event, specifying how loud the entity wants to be and what specific special channel they want to talk on (if any)
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
/// <param name="volume">How loud the message is</param>
/// <param name="channel">What specific channel to send the message on, if any</param>
[Serializable, NetSerializable]
public sealed class AttemptVerbalChatEvent(NetEntity sender, string message, VerbalVolume volume, ProtoId<RadioChannelPrototype>? channel) : ChatAttemptEvent(sender, message)
{
    public ProtoId<RadioChannelPrototype>? Channel = channel;
    public VerbalVolume Volume = volume;
}

/// <summary>
/// Raised when a mob tries to emote.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptEmoteEvent(NetEntity sender, string message) : ChatAttemptEvent(sender, message);

/// <summary>
/// Attempt an announcement via a communications console.
/// </summary>
/// <param name="console">The console sending the message</param>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptAnnouncementEvent(NetEntity sender, string message, NetEntity console) : ChatAttemptEvent(sender, message)
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
public sealed class AttemptOutOfCharacterChatEvent(NetEntity sender, string message, OutOfCharacterChatChannel channel) : ChatAttemptEvent(sender, message)
{
    public OutOfCharacterChatChannel Channel = channel;
}

#endregion

#region Failure Events

/// <summary>
/// Defines the abstract concept of chat attempts failing.
/// </summary>
/// <param name="sender">The speaker</param>
/// <param name="reason">Why the attempt failed</param>
[Serializable, NetSerializable]
public abstract class ChatFailedEvent : EntityEventArgs
{
    public ChatContext Context;
    public NetEntity Sender;
    public string? Reason;
}

/// <summary>
/// Raised when a character has failed to speak.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class VerbalChatFailedEvent : ChatFailedEvent;

/// <summary>
/// Raised when a mob has failed to emote.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class EmoteFailedEvent : ChatFailedEvent;

/// <summary>
/// Raised when an announcement is attempted by a communications console, and it fails for some reason.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class AnnouncementFailedEvent : ChatFailedEvent;

public sealed class OutOfCharacterChatFailed : ChatFailedEvent;

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
public abstract class ChatSuccessEvent(NetEntity speaker, string asName, string message, uint id) : EntityEventArgs
{
    public ChatContext Context;
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
    NetEntity speaker,
    string asName,
    string message,
    uint id,
    VerbalVolume volume) : ChatSuccessEvent(speaker, asName, message, id)
{
    public VerbalVolume Volume = volume;
}

/// <summary>
/// Raised when a mob emotes.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
[Serializable, NetSerializable]
public sealed class EmoteEvent(NetEntity speaker, string asName, string message, uint id) : ChatSuccessEvent(speaker, asName, message, id);

/// <summary>
/// Raised when an announcement is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class AnnouncementEvent(string asName, string message, Color? messageColorOverride = null) : EntityEventArgs
{
    public string AsName = asName;
    public readonly string Message = message;
    public Color? MessageColorOverride = messageColorOverride;
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
public sealed class RadioEvent(NetEntity speaker, string asName, string message, ProtoId<RadioChannelPrototype> channel, uint id) : ChatSuccessEvent(speaker, asName, message, id)
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
public sealed class BackgroundChatEvent(NetEntity speaker, string message, string asName, VerbalVolume volume) : EntityEventArgs
{
    public ChatContext Context;
    public NetEntity Speaker = speaker;
    public string AsName = asName;
    public readonly string Message = message;
    public VerbalVolume Volume = volume;
}

#endregion
