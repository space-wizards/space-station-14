using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Prototypes;
using Content.Shared.Chat.V2.Systems;
using Content.Shared.Radio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Chat.V2.Systems;

/// <summary>
/// Notifies that a chat attempt input from a player has been validated, ready for further processing.
/// </summary>
/// <param name="ev"></param>
public sealed class ChatCreatedEvent<T>(T ev) : EntityEventArgs where T : CreatedChatEvent
{
    public CreatedChatEvent Event = ev;
}

/// <summary>
/// Notifies that a chat message needs validating.
/// </summary>
/// <param name="attemptEvent">The chat message to validate</param>
[ByRefEvent]
public sealed class ChatSentEvent<T>(T attemptEvent) where T : SendableChatEvent
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
public sealed class ChatSanitizationEvent<T>(T attemptEvent) where T : ICreatedChatEvent
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

/// <summary>
/// Notifies that a chat message has been created.
/// </summary>
/// <param name="ev"></param>
public sealed class MessageCreatedEvent<T>(T ev) : EntityEventArgs where T : CreatedChatEvent
{
    public T Event = ev;
}

[ByRefEvent]
public sealed class GeneralChatMutationEvent<T>(T ev) : EntityEventArgs where T : CreatedChatEvent
{
    public T Event = ev;
}

[ByRefEvent]
public sealed class ChatTargetCalculationEvent<T>(T ev) : EntityEventArgs where T : CreatedChatEvent
{
    public T Event = ev;
    public IList<ICommonSession> Targets = new List<ICommonSession>();
}

[ByRefEvent]
public sealed class ChatSpecificMutationEvent<T>(T ev, ICommonSession target) : EntityEventArgs where T : CreatedChatEvent
{
    public T Event = ev;
    public ICommonSession Target = target;
}
