using Content.Shared.Chat.Prototypes;
using Content.Shared.Chat.V2;
using Content.Shared.Radio;

namespace Content.Server.Chat.V2;

/// <summary>
/// Raised when a comms console makes an announcement.
/// </summary>
public sealed class CommsAnnouncementCreatedEvent(EntityUid sender, EntityUid console, string message) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = sender;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.Announcement;
    public EntityUid Console = console;
}

/// <summary>
/// Raised locally when a character speaks in Dead Chat.
/// </summary>
public sealed class DeadChatCreatedEvent(EntityUid speaker, string message, bool isAdmin) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = speaker;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.DeadChat;
    public bool IsAdmin = isAdmin;
}

/// <summary>
/// Raised by chat system when entity made some emote.
/// Use it to play sound, change sprite or something else.
/// </summary>
public sealed class EmoteCreatedEvent(EntityUid sender, string message, float range) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = sender;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.Emote;
    public float Range = range;
}

/// <summary>
/// A server-only event that is fired when an entity chats in local chat.
/// </summary>
public sealed class LocalChatCreatedEvent(EntityUid speaker, string message, float range) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = speaker;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.Local;
    public float Range = range;
}

/// <summary>
/// Raised when a character speaks in LOOC.
/// </summary>
public sealed class LoocCreatedEvent : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; }
    public string Message { get; set; }
    public MessageType Type => MessageType.Looc;

    public LoocCreatedEvent(EntityUid speaker, string message)
    {
        Sender = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
public sealed class RadioCreatedEvent(
    EntityUid speaker,
    string message,
    RadioChannelPrototype channel)
    : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = speaker;
    public string Message { get; set; } = message;
    public RadioChannelPrototype Channel = channel;
    public MessageType Type => MessageType.Radio;
}

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
[Serializable]
public sealed class RadioEmittedEvent : EntityEventArgs
{
    public EntityUid Speaker;
    public EntityUid? Device;
    public string AsName;
    public string Message;
    public string Channel;
    public uint Id;

    public RadioEmittedEvent(
        EntityUid speaker,
        string asName,
        string message,
        string channel,
        EntityUid? device,
        uint id
    )
    {
        Speaker = speaker;
        Device = device;
        AsName = asName;
        Message = message;
        Channel = channel;
        Id = id;
    }

    public string GetMessage()
    {
        return Message;
    }
}

/// <summary>
/// Raised when a character whispers.
/// </summary>
public sealed class WhisperCreatedEvent(EntityUid speaker, string message, float minRange, float maxRange) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = speaker;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.Whisper;
    public float MinRange = minRange;
    public float MaxRange = maxRange;
}

