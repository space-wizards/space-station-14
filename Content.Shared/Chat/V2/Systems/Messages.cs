using System.Runtime.InteropServices;
using Content.Shared.Chat.V2.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chat.V2.Systems;

/// <summary>
/// Defines process-scoped context for a chat message, allowing for custom data for atypical chat channels and circumstances.
/// </summary>
[Serializable, NetSerializable]
public struct ChatContext
{
    public Dictionary<string, object> Values;

    public ChatContext Clone()
    {
        var outCtx = new ChatContext();
        outCtx.Values = Values.ShallowClone();

        return outCtx;
    }
}

#region Attempt Events

/// <summary>
/// Defines the abstract concept of a chat attempt.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent.</param>
[Serializable, NetSerializable]
public abstract class ChatAttemptEvent(ChatContext context, ICommonSession session, NetEntity sender, string message) : EntityEventArgs
{
    public ChatContext Context = context;
    public ICommonSession SenderSession = session;
    public NetEntity Sender = sender;
    public string Message = message;

    public abstract ChatFailedEvent ToFailMessage(string reason);
    public abstract ChatEvent ToCreatedEvent(string asName);
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
    ICommonSession session,
    NetEntity sender,
    string message,
    ProtoId<VerbalChatChannelPrototype> chatChannel,
    ProtoId<RadioChannelPrototype>? radioChannel
) : ChatAttemptEvent(context, session, sender, message)
{
    public ProtoId<RadioChannelPrototype>? RadioChannel = radioChannel;
    public ProtoId<VerbalChatChannelPrototype> ChatChannel = chatChannel;

    public override VerbalChatFailedEvent ToFailMessage(string reason)
    {
        return new VerbalChatFailedEvent(Context, Sender, reason);
    }

    public override VerbalChatCreatedEvent ToCreatedEvent(string asName)
    {
        return new VerbalChatCreatedEvent(Context, SenderSession, Sender, ChatChannel, RadioChannel, Message, asName);
    }
}

/// <summary>
/// Raised when a mob tries to emote.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
[Serializable, NetSerializable]
public sealed class AttemptVisualChatEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    ProtoId<VisualChatChannelPrototype> channel,
    string message
) : ChatAttemptEvent(context, session, sender, message)
{
    public ProtoId<VisualChatChannelPrototype> Channel = channel;

    public override VisualChatFailedEvent ToFailMessage(string reason)
    {
        return new VisualChatFailedEvent(Context, Sender, reason);
    }

    public override VisualChatCreatedEvent ToCreatedEvent(string asName)
    {
        return new VisualChatCreatedEvent(Context, SenderSession, Sender, Channel, Message, asName);
    }
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
    ICommonSession session,
    NetEntity sender,
    string message,
    NetEntity console
) : ChatAttemptEvent(context, session, sender, message)
{
    public NetEntity Console = console;

    public override AnnouncementFailedEvent ToFailMessage(string reason)
    {
        return new AnnouncementFailedEvent(Context, Sender, reason);
    }

    public override AnnouncementCreatedEvent ToCreatedEvent(string asName)
    {
        return new AnnouncementCreatedEvent(Context, SenderSession, Sender, Console, Message, asName);
    }
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
    ICommonSession session,
    NetEntity sender,
    string message,
    ProtoId<OutOfCharacterChannelPrototype> channel
) : ChatAttemptEvent(context, session, sender, message)
{
    public ProtoId<OutOfCharacterChannelPrototype> Channel = channel;

    public override OutOfCharacterChatFailed ToFailMessage(string reason)
    {
        return new OutOfCharacterChatFailed(Context, Sender, reason);
    }

    public override OutOfCharacterChatCreatedEvent ToCreatedEvent(string asName)
    {
        return new OutOfCharacterChatCreatedEvent(Context, SenderSession, Sender, Channel, Message, asName);
    }
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
    ProtoId<VerbalChatChannelPrototype> chatChannel
) : ChatSuccessEvent(context, speaker, asName, message, id)
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
public sealed class VisualChatEvent(
    ChatContext context,
    NetEntity speaker,
    string asName,
    string message,
    uint id,
    ProtoId<VisualChatChannelPrototype>  chatChannel
) : ChatSuccessEvent(context, speaker, asName, message, id)
{
    public ProtoId<VisualChatChannelPrototype> ChatChannel = chatChannel;
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
    ProtoId<OutOfCharacterChannelPrototype>  chatChannel
) : ChatSuccessEvent(context, speaker, asName, message, id)
{
    public ProtoId<OutOfCharacterChannelPrototype>  ChatChannel = chatChannel;
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

#endregion

#region Validated Events
/// <summary>
/// Notifies that a chat attempt input from a player has been validated, ready for further processing.
/// </summary>
/// <param name="ev"></param>
public sealed class ChatAttemptValidatedEvent<T>(T ev) : EntityEventArgs where T : ChatEvent
{
    public ChatEvent Event = ev;
}

/// <summary>
/// Notifies that a chat message needs validating.
/// </summary>
/// <param name="attemptEvent">The chat message to validate</param>
[ByRefEvent]
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

public abstract class ChatEvent(ChatContext context, ICommonSession session, NetEntity sender, string message, string asName) : IChatEvent
{
    public ChatContext Context { get; } = context;
    public NetEntity Sender { get; set; } = sender;
    public string Message { get; set; } = message;
    public uint Id { get; set; }
    public string AsName { get; set; } = asName;
    public ICommonSession SenderSession { get; } = session;
    public abstract ChatSuccessEvent ToSuccessEvent();
    public abstract IChatEvent Clone();
}

/// <summary>
/// Raised locally when a comms announcement is made.
/// </summary>
public sealed class AnnouncementCreatedEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    NetEntity console,
    string message,
    string asName
) : ChatEvent(context, session, sender, message, asName)
{
    public NetEntity Console = console;

    public override ChatSuccessEvent ToSuccessEvent()
    {
        return new AnnouncementEvent(Context, AsName, Message, Id, null);
    }

    public override AnnouncementCreatedEvent Clone()
    {
        return new AnnouncementCreatedEvent(Context.Clone(), SenderSession, Sender, Console, Message, AsName);
    }
}

/// <summary>
/// Raised locally when an OOC message is created.
/// </summary>
public sealed class OutOfCharacterChatCreatedEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    ProtoId<OutOfCharacterChannelPrototype> channel,
    string message,
    string asName
) : ChatEvent(context, session, sender, message, asName)
{
    public ProtoId<OutOfCharacterChannelPrototype> Channel = channel;

    public override ChatSuccessEvent ToSuccessEvent()
    {
        return new OutOfCharacterChatEvent(Context, Sender, AsName, Message, Id, Channel);
    }

    public override OutOfCharacterChatCreatedEvent Clone()
    {
        return new OutOfCharacterChatCreatedEvent(Context.Clone(), SenderSession, Sender, Channel, Message, AsName);
    }
}

/// <summary>
/// Raised locally when a character emotes.
/// </summary>
public sealed class VisualChatCreatedEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    ProtoId<VisualChatChannelPrototype> channel,
    string message,
    string asName
) : ChatEvent(context, session, sender, message, asName)
{
    public ProtoId<VisualChatChannelPrototype> Channel = channel;

    public override ChatSuccessEvent ToSuccessEvent()
    {
        return new VisualChatEvent(Context.Clone(), Sender, AsName, Message, Id, Channel);
    }

    public override VisualChatCreatedEvent Clone()
    {
        return new VisualChatCreatedEvent(Context.Clone(), SenderSession, Sender, Channel, Message, AsName);
    }
}

/// <summary>
/// Raised locally when something talks.
/// </summary>
public sealed class VerbalChatCreatedEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    ProtoId<VerbalChatChannelPrototype> chatChannel,
    ProtoId<RadioChannelPrototype>? radioChannel,
    string message,
    string asName
) : ChatEvent(context, session, sender, message, asName)
{
    public ProtoId<RadioChannelPrototype>? RadioChannel = radioChannel;
    public ProtoId<VerbalChatChannelPrototype> ChatChannel = chatChannel;

    public override ChatSuccessEvent ToSuccessEvent()
    {
        return new VerbalChatEvent(Context.Clone(), Sender, AsName, Message, Id, ChatChannel);
    }

    public override VerbalChatCreatedEvent Clone()
    {
        return new VerbalChatCreatedEvent(Context.Clone(), SenderSession, Sender, ChatChannel, RadioChannel, Message, AsName);
    }
}

#endregion

#region Core Message Events

/// <summary>
/// Notifies that a chat message has been created.
/// </summary>
/// <param name="ev"></param>
[Serializable, NetSerializable]
public sealed class MessageCreatedEvent<T>(T ev) : EntityEventArgs where T : ChatEvent
{
    public T Event = ev;
}

/// <summary>
/// Notifies that a chat message has been changed.
/// </summary>
/// <param name="id"></param>
/// <param name="newMessage"></param>
[Serializable, NetSerializable]
public sealed class MessagePatchedEvent(uint id, string newMessage) : EntityEventArgs
{
    public uint MessageId = id;
    public string NewMessage = newMessage;
}

/// <summary>
/// Notifies that a chat message has been deleted.
/// </summary>
/// <param name="id"></param>
[Serializable, NetSerializable]
public sealed class MessageDeletedEvent(uint id) : EntityEventArgs
{
    public uint MessageId = id;
}

/// <summary>
/// Notifies that a player's messages have been nuked.
/// </summary>
/// <param name="set"></param>
[Serializable, NetSerializable]
public sealed class MessagesNukedEvent(List<uint> set) : EntityEventArgs
{
    public uint[] MessageIds = CollectionsMarshal.AsSpan(set).ToArray();
}

#endregion

#region Post-Creation events

[ByRefEvent]
public sealed class GeneralChatMutationEvent<T>(T ev) : EntityEventArgs where T : ChatEvent
{
    public T Event = ev;
}

[ByRefEvent]
public sealed class ChatTargetCalculationEvent<T>(T ev) : EntityEventArgs where T : ChatEvent
{
    public T Event = ev;
    public IList<ICommonSession> Targets = new List<ICommonSession>();
}

[ByRefEvent]
public sealed class ChatSpecificMutationEvent<T>(T ev, ICommonSession target) : EntityEventArgs where T : ChatEvent
{
    public T Event = ev;
    public ICommonSession Target = target;
}

#endregion
