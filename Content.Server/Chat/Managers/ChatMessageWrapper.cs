using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Managers;

/// <summary> Wrapper for general chat message info. </summary>
public record ChatMessageWrapper
{
    /// <summary>Creates chat message wrapper from parts.</summary>
    /// <param name="messageId">Unique message id. Can be used to send 'delete' message to all clients.</param>
    /// <param name="messageContent">The message that is to be sent.</param>
    ///  <param name="communicationChannel">The communication channel prototype to use as a base for how the message should be treated.</param>
    ///  <param name="senderSession">The session that sends the message. If null, this means the server is sending the message.</param>
    ///  <param name="senderEntity">The entity designated as "sending" the message. If null, the message is coming directly from the session/server.</param>
    ///  <param name="parent">Message, that led to publishing of this message.</param>
    ///  <param name="targetSessions">
    /// Any sessions that should be specifically targeted (still needs to comply with channel consume conditions).
    /// If you are targeting multiple sessions you should likely use a consumeCollection instead of this.
    /// </param>
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

    /// <summary> Creates clone of chat message wrapper. </summary>
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
    }

    /// <summary> Creates clone of chat message wrapper but with other communication channel. </summary>
    public ChatMessageWrapper(ChatMessageWrapper other, CommunicationChannelPrototype communicationChannel) : this(other)
    {
        CommunicationChannel = communicationChannel;
    }

    /// <summary> Unique message id. Can be used to send 'delete' message to all clients. </summary>
    public uint MessageId { get; }

    /// <summary>The message that is to be sent.</summary>
    public FormattedMessage MessageContent { get; private set; }

    /// <summary>The communication channel prototype to use as a base for how the message should be treated.</summary>
    public CommunicationChannelPrototype CommunicationChannel { get; }

    /// <summary>The session that sends the message. If null, this means the server is sending the message.</summary>
    public ICommonSession? SenderSession { get; }

    /// <summary>The entity designated as "sending" the message. If null, the message is coming directly from the session/server.</summary>
    public EntityUid? SenderEntity { get; }

    /// <summary>Message, that led to publishing of this message.</summary>
    public ChatMessageWrapper? Parent { get; }

    /// <summary>
    /// Any sessions that should be specifically targeted (still needs to comply with channel consume conditions).
    /// If you are targeting multiple sessions you should likely use a consumeCollection instead of this.
    /// </summary>
    public HashSet<ICommonSession>? TargetSessions { get; }

    /// <summary>Parameters that may be used by ChatModifiers; these are not passed on to the client, though may be set again clientside.</summary>
    public ChatMessageContext? Context { get; }

    /// <summary> Change message formatted text. </summary>
    public void SetMessage(FormattedMessage message)
    {
        MessageContent = message;
    }
}
