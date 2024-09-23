using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

/// <summary>
/// Chat message sent from client->server
/// </summary>
public sealed class RequestChatMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public string Text = default!;
    public ChatSelectChannel Channel;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Text = buffer.ReadString();
        Channel = (ChatSelectChannel)buffer.ReadUInt16();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Text);
        buffer.Write((ushort)Channel);
    }
}
