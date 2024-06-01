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
public sealed class ChatAttemptValidatedEvent(IChatEvent ev) : EntityEventArgs
{
    public IChatEvent Event = ev;
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
public sealed class ChatSanitizationEvent<T>(T attemptEvent) where T : IChatEvent
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

public abstract class ChatEvent(ChatContext context, EntityUid sender, string message) : IChatEvent
{
    public ChatContext Context { get; } = context;
    public EntityUid Sender { get; set; } = sender;
    public string Message { get; set; } = message;
    public uint Id { get; set; }
}

/// <summary>
/// Raised locally when a comms announcement is made.
/// </summary>
public sealed class CommsAnnouncementCreatedEvent(
    ChatContext context,
    EntityUid sender,
    EntityUid console,
    string message
) : ChatEvent(context, sender, message)
{
    public EntityUid Console = console;
}

/// <summary>
/// Raised locally when an OOC message is created.
/// </summary>
public sealed class OutOfCharacterChatCreatedEvent(
    ChatContext context,
    EntityUid sender,
    OutOfCharacterChatChannel channel,
    string message
) : ChatEvent(context, sender, message)
{
    public OutOfCharacterChatChannel Channel = channel;
}

/// <summary>
/// Raised locally when a character emotes.
/// </summary>
public sealed class VisualChatCreatedEvent(
    ChatContext context,
    EntityUid sender,
    VisualChatChannel channel,
    string message
) : ChatEvent(context, sender, message)
{
    public VisualChatChannel Channel = channel;
}

/// <summary>
/// Raised locally when something talks.
/// </summary>
public sealed class VerbalChatCreatedEvent(
    ChatContext context,
    EntityUid sender,
    VerbalChatChannel chatChannel,
    ProtoId<RadioChannelPrototype>? radioChannel,
    string message
) : ChatEvent(context, sender, message)
{
    public ProtoId<RadioChannelPrototype>? RadioChannel = radioChannel;
    public VerbalChatChannel ChatChannel = chatChannel;
}
