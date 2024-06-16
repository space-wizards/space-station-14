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

public abstract class CreatedChatEvent(ChatContext context, ICommonSession session, NetEntity sender, string message, string asName) : ICreatedChatEvent
{
    public ChatContext Context { get; } = context;
    public NetEntity Sender { get; set; } = sender;
    public string Message { get; set; } = message;
    public uint Id { get; set; }
    public string AsName { get; set; } = asName;
    public ICommonSession SenderSession { get; } = session;
    public abstract ChatReceivedEvent ToReceivedEvent();
    public abstract ICreatedChatEvent Clone();
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
) : CreatedChatEvent(context, session, sender, message, asName)
{
    public NetEntity Console = console;

    public override ChatReceivedEvent ToReceivedEvent()
    {
        return new AnnouncementReceivedEvent(Context, AsName, Message, Id, null);
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
) : CreatedChatEvent(context, session, sender, message, asName)
{
    public ProtoId<OutOfCharacterChannelPrototype> Channel = channel;

    public override ChatReceivedEvent ToReceivedEvent()
    {
        return new OutOfCharacterChatReceivedEvent(Context, Sender, AsName, Message, Id, Channel);
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
) : CreatedChatEvent(context, session, sender, message, asName)
{
    public ProtoId<VisualChatChannelPrototype> Channel = channel;

    public override ChatReceivedEvent ToReceivedEvent()
    {
        return new VisualChatRecievedEvent(Context.Clone(), Sender, AsName, Message, Id, Channel);
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
) : CreatedChatEvent(context, session, sender, message, asName)
{
    public ProtoId<RadioChannelPrototype>? RadioChannel = radioChannel;
    public ProtoId<VerbalChatChannelPrototype> ChatChannel = chatChannel;

    public override ChatReceivedEvent ToReceivedEvent()
    {
        return new VerbalChatReceivedEvent(Context.Clone(), Sender, AsName, Message, Id, ChatChannel);
    }

    public override VerbalChatCreatedEvent Clone()
    {
        return new VerbalChatCreatedEvent(Context.Clone(), SenderSession, Sender, ChatChannel, RadioChannel, Message, AsName);
    }
}
