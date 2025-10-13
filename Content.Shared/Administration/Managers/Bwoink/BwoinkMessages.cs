using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Managers.Bwoink;

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

        Message = new BwoinkMessage(sender, null, sentAt, message, (MessageFlags)buffer.ReadByte());
        Channel = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        DebugTools.AssertNotNull(Message, "Message should have a value");
        DebugTools.AssertNotEqual(Channel.Id, string.Empty, "Channel must be set");

        buffer.Write(Message.Sender);
        buffer.Write(Message.SentAt.ToBinary());
        buffer.Write(Message.Content);
        buffer.Write((byte)Message.Flags);

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

        Message = new BwoinkMessage(sender, senderId, sentAt, message, (MessageFlags)buffer.ReadByte());
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
        buffer.Write((byte)Message.Flags);

        buffer.Write(Channel.Id);
        buffer.Write(Target.UserId);
    }
}
