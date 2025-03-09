using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Managers;

/// <summary></summary>
public record ChatMessageWrapper
{
    /// <summary></summary>
    /// <param name="messageId"></param>
    /// <param name="messageContent">The message that is to be sent.</param>
    ///  <param name="communicationChannel">The communication channel prototype to use as a base for how the message should be treated.</param>
    ///  <param name="senderSession">The session that sends the message. If null, this means the server is sending the message.</param>
    ///  <param name="senderEntity">The entity designated as "sending" the message. If null, the message is coming directly from the session/server.</param>
    ///  <param name="parent">.</param>
    ///  <param name="targetSessions">Any sessions that should be specifically targetted (still needs to comply with channel consume conditions). If you are targetting multiple sessions you should likely use a consumeCollection instead of this.</param>
    ///  <param name="context">Parameters that may be used by ChatModifiers; these are not passed on to the client, though may be set again clientside.</param>
    public ChatMessageWrapper(
        uint messageId,
        FormattedMessage messageContent,
        CommunicationChannelPrototype communicationChannel,
        ICommonSession? senderSession,
        EntityUid? senderEntity,
        ChatMessageWrapper? parent,
        HashSet<ICommonSession>? targetSessions = null,
        ChatMessageContext? context = null
    )
    {
        MessageId = messageId;
        MessageContent = messageContent;
        CommunicationChannel = communicationChannel;
        SenderSession = senderSession;
        SenderEntity = senderEntity;
        Parent = parent;
        TargetSessions = targetSessions;
        Context = context;
    }

    public ChatMessageWrapper(ChatMessageWrapper other)
    {
        Parent = other;

        MessageContent = other.MessageContent.Nodes.Count > 0
            ? FormattedMessage.FromNodes(other.MessageContent.Nodes)
            : FormattedMessage.Empty;
        CommunicationChannel = other.CommunicationChannel;
        SenderSession = other.SenderSession;
        SenderEntity = other.SenderEntity;
        TargetSessions = other.TargetSessions;
        Context = other.Context;
        LogMessage = other.LogMessage;
    }

    public ChatMessageWrapper(ChatMessageWrapper other, CommunicationChannelPrototype communicationChannel) : this(other)
    {
        CommunicationChannel = communicationChannel;
    }

    public uint MessageId { get; }

    /// <summary>The message that is to be sent.</summary>
    public FormattedMessage MessageContent { get; private set; }

    /// <summary>The communication channel prototype to use as a base for how the message should be treated.</summary>
    public CommunicationChannelPrototype CommunicationChannel { get; }

    /// <summary>The session that sends the message. If null, this means the server is sending the message.</summary>
    public ICommonSession? SenderSession { get; }

    /// <summary>The entity designated as "sending" the message. If null, the message is coming directly from the session/server.</summary>
    public EntityUid? SenderEntity { get; }

    /// <summary>.</summary>
    public ChatMessageWrapper? Parent { get; }

    /// <summary>Any sessions that should be specifically targetted (still needs to comply with channel consume conditions). If you are targetting multiple sessions you should likely use a consumeCollection instead of this.</summary>
    public HashSet<ICommonSession>? TargetSessions { get; }

    /// <summary>Parameters that may be used by ChatModifiers; these are not passed on to the client, though may be set again clientside.</summary>
    public ChatMessageContext? Context { get; }

    /// <summary>Whether the message should be logged in the admin logs. Defaults to true.</summary>
    public bool LogMessage { get; }

    public void SetMessage(FormattedMessage message)
    {
        MessageContent = message;
    }
}
