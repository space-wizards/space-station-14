using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Systems;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.V2;

/// <summary>
/// Notifies that a chat attempt input from a player has been validated and sanatized, ready for further processing.
/// </summary>
/// <param name="ev"></param>
public sealed class ChatAttemptValidatedEvent(ChatAttemptEvent ev) : EntityEventArgs
{
    public ChatAttemptEvent Event = ev;
}

/// <summary>
/// Notifies that a chat message needs validating.
/// </summary>
/// <param name="attemptEvent">The chat message to validate</param>
public sealed class ChatValidationEvent<T>(T attemptEvent) where T : ChatAttemptEvent
{
    public readonly T Event = attemptEvent;
    public string Reason = "";

    public bool IsCancelled { get; private set; }

    public void Cancel(string reason)
    {
        if (IsCancelled)
        {
            return;
        }

        IsCancelled = true;
        Reason = reason;
    }
}

/// <summary>
/// Notifies that a chat message needs sanitizing. If, after this message is processed, IsCancelled is true, the message
/// should be discarded with a failure response. Otherwise, if ChatMessageSanitized is non-null, ChatMessageSanitized
/// should be used instead of the non-sanitized message.
/// </summary>
/// <param name="attemptEvent">The chat message to sanitize.</param>
public sealed class ChatSanitizationEvent<T>(T attemptEvent) where T : ChatAttemptEvent
{
    public string ChatMessageRaw = attemptEvent.Message;
    public bool IsCancelled;
    public string? ChatMessageSanitized { get; private set; }

    // Commits the sanitized message string to the event. If a message string has already been input, this is a no-op.
    public void Sanitize(string inMessage)
    {
        if (ChatMessageSanitized != null)
        {
            return;
        }

        ChatMessageSanitized = inMessage;
    }
}

public abstract class ChatEvent(uint id, ChatContext context, EntityUid sender, string message, MessageType type) : IChatEvent
{
    public ChatContext Context { get; } = context;
    public EntityUid Sender { get; set; } = sender;
    public string Message { get; set; } = message;
    public MessageType Type { get; } = type;
    public uint Id { get; set; } = id;
}

// /// <summary>
// /// Raised locally when a comms announcement is made.
// /// </summary>
// public sealed class CommsAnnouncementCreatedEvent(uint id, ChatContext context, EntityUid sender, EntityUid console, string message) : ChatEvent(id, context, sender, message, MessageType.Announcement)
// {
//     public EntityUid Console = console;
// }
//
// /// <summary>
// /// Raised locally when a character speaks in Dead Chat.
// /// </summary>
// public sealed class DeadChatCreatedEvent(uint id, ChatContext context, EntityUid speaker, string message, bool isAdmin) : ChatEvent(id, context, speaker, message, MessageType.DeadChat)
// {
//     public bool IsAdmin = isAdmin;
// }
//
// /// <summary>
// /// Raised locally when a character emotes.
// /// </summary>
// public sealed class EmoteCreatedEvent(uint id, ChatContext context, EntityUid sender, string message, float range) : ChatEvent(id, context, sender, message, MessageType.Emote)
// {
//     public float Range = range;
// }
//
// /// <summary>
// /// Raised locally when a character talks in local.
// /// </summary>
// public sealed class VerbalChatCreatedEvent(uint id, ChatContext context, EntityUid speaker, string message, float range) : ChatEvent
// {
//     public ChatContext Context { get; } = context;
//     public uint Id { get; set; }
//     public EntityUid Sender { get; set; } = speaker;
//     public string Message { get; set; } = message;
//     public MessageType Type => MessageType.Local;
//     public float Range = range;
//     public VerbalVolume Volume;
// }
//
// /// <summary>
// /// Raised locally when a character speaks in LOOC.
// /// </summary>
// public sealed class LoocCreatedEvent(uint id, ChatContext context, EntityUid speaker, string message) : ChatEvent
// {
//     public ChatContext Context { get; } = context;
//     public uint Id { get; set; }
//     public EntityUid Sender { get; set; } = speaker;
//     public string Message { get; set; } = message;
//     public MessageType Type => MessageType.Looc;
// }
//
// /// <summary>
// /// Raised locally when a character speaks on the radio.
// /// </summary>
// public sealed class RadioCreatedEvent(uint id, ChatContext context, EntityUid speaker, string message, ProtoId<RadioChannelPrototype> channel) : ChatEvent
// {
//     public ChatContext Context { get; } = context;
//     public uint Id { get; set; }
//     public EntityUid Sender { get; set; } = speaker;
//     public string Message { get; set; } = message;
//     public ProtoId<RadioChannelPrototype> Channel = channel;
//     public MessageType Type => MessageType.Radio;
// }
//
// /// <summary>
// /// Raised locally when a character whispers.
// /// </summary>
// public sealed class WhisperCreatedEvent(uint id, ChatContext context, EntityUid speaker, string message, float minRange, float maxRange) : ChatEvent
// {
//     public uint Id { get; set; }
//     public EntityUid Sender { get; set; } = speaker;
//     public string Message { get; set; } = message;
//     public MessageType Type => MessageType.Whisper;
//     public float MinRange = minRange;
//     public float MaxRange = maxRange;
// }
//
