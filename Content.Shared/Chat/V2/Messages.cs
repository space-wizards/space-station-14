using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

#region Attempt Messages

/// <summary>
/// Defines the abstract concept of a chat attempt.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent.</param>
[Serializable, NetSerializable]
public abstract class ChatAttemptEvent(NetEntity sender, string message) : EntityEventArgs
{
    public NetEntity Sender = sender;
    public string Message = message;
}

/// <summary>
/// /// Raised when an announcement is attempted by a communications console.
/// </summary>
/// <param name="console">The console sending the message</param>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptCommsAnnouncementEvent(NetEntity sender, string message, NetEntity console) : ChatAttemptEvent(sender, message)
{
    public NetEntity Console = console;
}

/// <summary>
/// Raised when a mob tries to speak in dead chat.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptDeadChatEvent(NetEntity sender, string message) : ChatAttemptEvent(sender, message);

/// <summary>
/// Raised when a mob tries to emote.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptEmoteEvent(NetEntity sender, string message) : ChatAttemptEvent(sender, message);

/// <summary>
/// Raised when a mob tries to speak in local chat.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptLocalChatEvent(NetEntity sender, string message) : ChatAttemptEvent(sender, message);

/// <summary>
/// Raised when a mob tries to use the radio.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
/// <param name="channel">The channel the message is on</param>
[Serializable, NetSerializable]
public abstract class RadioAttemptEvent(NetEntity sender, string message, ProtoId<RadioChannelPrototype> channel) : ChatAttemptEvent(sender, message)
{
    public ProtoId<RadioChannelPrototype> Channel = channel;
}

/// <summary>
/// Raised when a mob tries to use the radio via a headset or similar device.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
/// <param name="channel">The channel the message is on</param>
[Serializable, NetSerializable]
public sealed class AttemptEquipmentRadioEvent(NetEntity sender, string message, string channel)
    : RadioAttemptEvent(sender, message, channel);

/// <summary>
/// Raised when a mob tries to use the radio via their innate abilities.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
/// <param name="channel">The channel the message is on</param>
[Serializable, NetSerializable]
public sealed class AttemptInternalRadioEvent(NetEntity sender, string message, string channel) : RadioAttemptEvent(sender, message, channel);

/// <summary>
/// Raised when a mob tries to speak in LOOC.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptLoocEvent(NetEntity sender, string message) : ChatAttemptEvent(sender, message);

/// <summary>
/// Raised when a mob tries to whisper.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptWhisperEvent(NetEntity sender, string message) : ChatAttemptEvent(sender, message);

# endregion

#region Failure messages

/// <summary>
/// Defines the abstract concept of chat attempts failing.
/// </summary>
/// <param name="sender">The speaker</param>
/// <param name="reason">Why the attempt failed</param>
[Serializable, NetSerializable]
public abstract class ChatFailedEvent(NetEntity sender, string reason) : EntityEventArgs
{
    public NetEntity Sender = sender;
    public string Reason = reason;
}

/// <summary>
/// Raised when an announcement is attempted by a communications console, and it fails for some reason.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class CommsAnnouncementFailedEvent(NetEntity sender, string reason) : ChatFailedEvent(sender, reason);

/// <summary>
/// Raised when a character has failed to speak in Dead chat.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class DeadChatFailedEvent(NetEntity sender, string reason) : ChatFailedEvent(sender, reason);

/// <summary>
/// Raised when a mob has failed to emote.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class EmoteFailedEvent(NetEntity sender, string reason) : ChatFailedEvent(sender, reason);

/// <summary>
/// Raised when a character has failed to speak in local chat.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class LocalChatFailedEvent(NetEntity sender, string reason) : ChatFailedEvent(sender, reason);

/// <summary>
/// Raised when a character has failed to speak on the radio.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class RadioFailedEvent(NetEntity sender, string reason) : ChatFailedEvent(sender, reason);

/// <summary>
/// Raised when a character has failed to speak in LOOC.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class LoocFailedEvent(NetEntity sender, string reason) : ChatFailedEvent(sender, reason);

/// <summary>
/// Raised when a character has failed to whisper.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="reason">The failure reason</param>
[Serializable, NetSerializable]
public sealed class WhisperFailedEvent(NetEntity sender, string reason) : ChatFailedEvent(sender, reason);

# endregion

#region Success messages

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
    public NetEntity Speaker = speaker;
    public string AsName = asName;
    public readonly string Message = message;
    public uint Id = id;
}

/// <summary>
/// Raised on the network when a character speaks in dead chat.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
/// <param name="isAdmin">If the speaker is an admin or not</param>
[Serializable, NetSerializable]
public sealed class DeadChatEvent(NetEntity speaker, string asName, string message, uint id, bool isAdmin) : ChatSuccessEvent(speaker, asName, message, id)
{
    public bool IsAdmin = isAdmin;
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
/// Raised to inform clients that an entity has spoken in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class LocalChatEvent(NetEntity speaker, string asName, string message, uint id) : ChatSuccessEvent(speaker, asName, message, id);

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
/// <param name="channel">The channel the message is on</param>
[Serializable, NetSerializable]
public sealed class RadioEvent(NetEntity speaker, string asName, string message, string channel, uint id) : ChatSuccessEvent(speaker, asName, message, id)
{
    public readonly string Channel = channel;
}

/// <summary>
/// Raised when a character speaks in LOOC.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
[Serializable, NetSerializable]
public sealed class LoocEvent(NetEntity speaker, string asName, string message, uint id) : ChatSuccessEvent(speaker, asName, message, id);

/// <summary>
/// Raised when a character whispers.
/// </summary>
/// <param name="speaker">The speaker of the message</param>
/// <param name="asName">What name the speaker should present as</param>
/// <param name="message">The message, possibly altered from when it was sent</param>
/// <param name="id">The round-unique ID of the message</param>
[Serializable, NetSerializable]
public sealed class WhisperEmittedEvent(NetEntity speaker, string asName, string message, uint id) : ChatSuccessEvent(speaker, asName, message, id);

#endregion

#region Irregular success messages

/// <summary>
/// Raised when a mob is given a subtle message in local chat.
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
public sealed class BackgroundChatEvent(NetEntity speaker, string message, string asName) : EntityEventArgs
{
    public NetEntity Speaker = speaker;
    public string AsName = asName;
    public readonly string Message = message;
}

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
