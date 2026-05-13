using Content.Shared.Chat.Prototypes;
using Content.Shared.Chat.V2;
using Content.Shared.Radio;

namespace Content.Server.Chat.V2;

/// <summary>
/// Raised locally when a comms announcement is made.
/// </summary>
public sealed partial class CommsAnnouncementCreatedEvent(EntityUid sender, EntityUid console, string message) : IChatEvent
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
public sealed partial class DeadChatCreatedEvent(EntityUid speaker, string message, bool isAdmin) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = speaker;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.DeadChat;
    public bool IsAdmin = isAdmin;
}

/// <summary>
/// Raised locally when a character emotes.
/// </summary>
public sealed partial class EmoteCreatedEvent(EntityUid sender, string message, float range) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = sender;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.Emote;
    public float Range = range;
}

/// <summary>
/// Raised locally when a character talks in local.
/// </summary>
public sealed partial class LocalChatCreatedEvent(EntityUid speaker, string message, float range) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = speaker;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.Local;
    public float Range = range;
}

/// <summary>
/// Raised locally when a character speaks in LOOC.
/// </summary>
public sealed partial class LoocCreatedEvent(EntityUid speaker, string message) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = speaker;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.Looc;
}

/// <summary>
/// Raised locally when a character speaks on the radio.
/// </summary>
public sealed partial class RadioCreatedEvent(
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
/// Raised locally when a character whispers.
/// </summary>
public sealed partial class WhisperCreatedEvent(EntityUid speaker, string message, float minRange, float maxRange) : IChatEvent
{
    public uint Id { get; set; }
    public EntityUid Sender { get; set; } = speaker;
    public string Message { get; set; } = message;
    public MessageType Type => MessageType.Whisper;
    public float MinRange = minRange;
    public float MaxRange = maxRange;
}


