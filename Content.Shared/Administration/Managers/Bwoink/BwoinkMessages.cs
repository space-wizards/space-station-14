using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Managers.Bwoink;

/// <summary>
/// Message used by the client and server to synchronize the <see cref="SharedBwoinkManager.Conversations"/> dictionary.
/// </summary>
public sealed class MsgBwoinkSync : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public Dictionary<ProtoId<BwoinkChannelPrototype>, Dictionary<NetUserId, Conversation>> Conversations = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Conversations.Clear();

        var numChannels = buffer.ReadInt32();
        for (var i = 0; i < numChannels; i++)
        {
            var channelId = buffer.ReadString();
            var innerDict = new Dictionary<NetUserId, Conversation>();

            var numConversations = buffer.ReadInt32();
            for (var j = 0; j < numConversations; j++)
            {
                var userId = new NetUserId(buffer.ReadGuid());
                var who = new NetUserId(buffer.ReadGuid());

                var messages = new List<BwoinkMessage>();
                var numMessages = buffer.ReadInt32();

                for (var k = 0; k < numMessages; k++)
                {
                    var sender = buffer.ReadString();

                    NetUserId? senderId = null;
                    if (buffer.ReadBoolean())
                        senderId = new NetUserId(buffer.ReadGuid());

                    var sentAt = DateTime.FromBinary(buffer.ReadInt64());
                    var content = buffer.ReadString();
                    var flags = (MessageFlags)buffer.ReadByte();
                    var roundTime = buffer.ReadTimeSpan();
                    var roundId = buffer.ReadInt32();

                    Color? color = null;
                    if (buffer.ReadBoolean())
                        color = buffer.ReadColor();

                    messages.Add(new BwoinkMessage(sender, senderId, sentAt, content, flags, roundTime, roundId, color));
                }

                var conversation = new Conversation(who, messages);

                innerDict[userId] = conversation;
            }

            Conversations[new ProtoId<BwoinkChannelPrototype>(channelId)] = innerDict;
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Conversations.Count);

        foreach (var (key, conversations) in Conversations)
        {
            buffer.Write(key.Id);

            buffer.Write(conversations.Count);
            foreach (var (convKey, conversation) in conversations)
            {
                buffer.Write(convKey);
                buffer.Write(conversation.Who); // this is most likely not needed, but whatever.

                buffer.Write(conversation.Messages.Count);
                foreach (var message in conversation.Messages)
                {
                    buffer.Write(message.Sender);
                    buffer.Write(message.SenderId.HasValue);
                    if (message.SenderId.HasValue)
                        buffer.Write(message.SenderId.Value.UserId);
                    buffer.Write(message.SentAt.ToBinary());
                    buffer.Write(message.Content);
                    buffer.Write((byte)message.Flags);
                    buffer.Write(message.RoundTime);
                    buffer.Write(message.RoundId);

                    buffer.Write(message.Color.HasValue);
                    if (message.Color.HasValue)
                        buffer.Write(message.Color.Value);
                }
            }
        }
    }
}

/// <summary>
/// Message sent by a client to request the status of all the channels.
/// </summary>
public sealed class MsgBwoinkSyncChannelsRequest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {

    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {

    }
}

/// <summary>
/// Message sent by the server after a <see cref="MsgBwoinkSyncChannelsRequest"/>
/// Contains all the channels and the status of them.
/// </summary>
public sealed class MsgBwoinkSyncChannels : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public Dictionary<ProtoId<BwoinkChannelPrototype>, BwoinkChannelConditionFlags> Channels = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            Channels.Add(buffer.ReadString(), (BwoinkChannelConditionFlags)buffer.ReadByte());
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Channels.Count);
        foreach (var (channel, flags) in Channels)
        {
            buffer.Write(channel.Id);
            buffer.Write((byte)flags);
        }
    }
}

/// <summary>
/// Message sent by a client to request the most up to date :tm: conversations.
/// </summary>
public sealed class MsgBwoinkSyncRequest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {

    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {

    }
}

/// <summary>
/// Message sent by the server to give a client that list of all currently typing users.
/// </summary>
public sealed class MsgBwoinkTypings : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public ProtoId<BwoinkChannelPrototype> Channel { get; set; }
    public Dictionary<NetUserId, List<TypingStatus>> Typings { get; set; } = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Channel = buffer.ReadString();

        var amount = buffer.ReadInt32();
        for (var i = 0; i < amount; i++)
        {
            var userChannelId = new NetUserId(buffer.ReadGuid());
            Typings.Add(userChannelId, []);
            var statusCount = buffer.ReadInt32();
            for (var j = 0; j < statusCount; j++)
            {
                Typings[userChannelId].Add(new TypingStatus(new NetUserId(buffer.ReadGuid()), buffer.ReadTimeSpan(), buffer.ReadString()));
            }
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Channel.Id);

        buffer.Write(Typings.Count);
        foreach (var (userId, statuses) in Typings)
        {
            buffer.Write(userId.UserId);
            buffer.Write(statuses.Count);
            foreach (var typingStatus in statuses)
            {
                buffer.Write(typingStatus.TypingUser.UserId);
                buffer.Write(typingStatus.Timeout);
                buffer.Write(typingStatus.Username);
            }
        }
    }
}

/// <summary>
/// Message sent by a client to indicate its typing status.
/// </summary>
public sealed class MsgBwoinkTypingUpdate : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public bool IsTyping { get; set; }
    public ProtoId<BwoinkChannelPrototype> Channel { get; set; }
    public NetUserId ChannelUserId { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        IsTyping = buffer.ReadBoolean();
        Channel = buffer.ReadString();
        ChannelUserId = new NetUserId(buffer.ReadGuid());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(IsTyping);
        buffer.Write(Channel.Id);
        buffer.Write(ChannelUserId.UserId);
    }
}

/// <summary>
/// Message sent to the client for receiving a bwoink and sent by a client to try to send a message.
/// This is for non-admins.
/// </summary>
public sealed class MsgBwoinkNonAdmin : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public ProtoId<BwoinkChannelPrototype> Channel = default!;
    public BwoinkMessage Message = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var sender = buffer.ReadString();
        // The non admin clients don't get the sender id.
        var sentAt = DateTime.FromBinary(buffer.ReadInt64());
        var message = buffer.ReadString();
        var roundTime = buffer.ReadTimeSpan();
        var roundId = buffer.ReadInt32();
        var flags = (MessageFlags)buffer.ReadByte();

        Color? color = null;
        if (buffer.ReadBoolean())
            color = buffer.ReadColor();

        Message = new BwoinkMessage(sender, null, sentAt, message, flags, roundTime, roundId, color);
        Channel = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        DebugTools.AssertNotNull(Message, "Message should have a value");
        DebugTools.AssertNotEqual(Channel.Id, string.Empty, "Channel must be set");

        buffer.Write(Message.Sender);
        buffer.Write(Message.SentAt.ToBinary());
        buffer.Write(Message.Content);
        buffer.Write(Message.RoundTime);
        buffer.Write(Message.RoundId);
        buffer.Write((byte)Message.Flags);

        buffer.Write(Message.Color.HasValue);
        if (Message.Color.HasValue)
            buffer.Write(Message.Color.Value);

        buffer.Write(Channel.Id);
    }
}

/// <summary>
/// Message sent to an admin client for getting a bwoink and sent by the client for bwoinking another person.
/// </summary>
public sealed class MsgBwoink : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public ProtoId<BwoinkChannelPrototype> Channel;
    public NetUserId Target;
    public BwoinkMessage Message = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var sender = buffer.ReadString();

        NetUserId? senderId = null;
        if (buffer.ReadBoolean())
            senderId = new NetUserId(buffer.ReadGuid());
        var sentAt = DateTime.FromBinary(buffer.ReadInt64());
        var message = buffer.ReadString();
        var roundTime = buffer.ReadTimeSpan();
        var roundId = buffer.ReadInt32();
        var flags = (MessageFlags)buffer.ReadByte();
        Color? color = null;
        if (buffer.ReadBoolean())
            color = buffer.ReadColor();

        Message = new BwoinkMessage(sender, senderId, sentAt, message, flags, roundTime, roundId, color);
        Channel = new ProtoId<BwoinkChannelPrototype>(buffer.ReadString());

        Target = new NetUserId(buffer.ReadGuid());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        DebugTools.AssertNotNull(Message, "Message should have a value");
        DebugTools.AssertNotEqual(Target.UserId, Guid.Empty, "Target must be set");
        DebugTools.AssertNotEqual(Channel.Id, string.Empty, "Channel must be set");

        buffer.Write(Message.Sender);
        buffer.Write(Message.SenderId.HasValue);
        if (Message.SenderId.HasValue)
            buffer.Write(Message.SenderId.Value.UserId);
        buffer.Write(Message.SentAt.ToBinary());
        buffer.Write(Message.Content);
        buffer.Write(Message.RoundTime);
        buffer.Write(Message.RoundId);
        buffer.Write((byte)Message.Flags);

        buffer.Write(Message.Color.HasValue);
        if (Message.Color.HasValue)
            buffer.Write(Message.Color.Value);

        buffer.Write(Channel.Id);
        buffer.Write(Target.UserId);
    }
}
