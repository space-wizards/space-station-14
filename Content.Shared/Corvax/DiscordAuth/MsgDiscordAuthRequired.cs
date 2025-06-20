using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.DiscordAuth;

/// <summary>
/// Server sends this event to client on connect if Discord auth is required
/// </summary>
public sealed class MsgDiscordAuthRequired : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string AuthUrl = string.Empty;
    public byte[] QrCode = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        AuthUrl = buffer.ReadString();
        buffer.ReadPadBits();
        var length = buffer.ReadInt32();
        if (length == 0)
        {
            return;
        }
        QrCode = buffer.ReadBytes(length);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(AuthUrl);
        buffer.WritePadBits();
        buffer.Write((int)QrCode.Length);
        if (QrCode.Length == 0)
        {
            return;
        }
        buffer.Write(QrCode);
    }
}