using Content.Shared.Chat.V2.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2.Systems;

/// <summary>
/// Defines the abstract concept of a chat attempt.
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent.</param>
[Serializable, NetSerializable]
public abstract class SendableChatEvent(ChatContext context, ICommonSession session, NetEntity sender, string message) : EntityEventArgs
{
    public ChatContext Context = context;
    public ICommonSession SenderSession = session;
    public NetEntity Sender = sender;
    public string Message = message;

    public abstract ChatFailedEvent ToFailMessage(string reason);
    public abstract CreatedChatEvent ToCreatedEvent(string asName);
}

/// <summary>
/// Attempt a verbal chat event, specifying how loud the entity wants to be and what specific special channel they want to talk on (if any)
/// </summary>
/// <param name="sender">Who sent the message</param>
/// <param name="message">The message sent</param>
/// <param name="chatChannel">How loud the message is</param>
/// <param name="radioChannel">What specific channel to send the message on, if any</param>
[Serializable, NetSerializable]
public sealed class VerbalChatSentEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    string message,
    ProtoId<VerbalChatChannelPrototype> chatChannel,
    ProtoId<RadioChannelPrototype>? radioChannel
) : SendableChatEvent(context, session, sender, message)
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
public sealed class VisualChatSentEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    ProtoId<VisualChatChannelPrototype> channel,
    string message
) : SendableChatEvent(context, session, sender, message)
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
public sealed class AnnouncementSentEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    string message,
    NetEntity console
) : SendableChatEvent(context, session, sender, message)
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
public sealed class OutOfCharacterChatSentEvent(
    ChatContext context,
    ICommonSession session,
    NetEntity sender,
    string message,
    ProtoId<OutOfCharacterChannelPrototype> channel
) : SendableChatEvent(context, session, sender, message)
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
