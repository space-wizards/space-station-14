using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public sealed class NetworkResourceUploadMessage : NetMessage
{
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public byte[] Data { get; set; } = Array.Empty<byte>();
    public ResPath RelativePath { get; set; } = ResPath.Self;

    public NetworkResourceUploadMessage()
    {
    }

    public NetworkResourceUploadMessage(byte[] data, ResPath relativePath)
    {
        Data = data;
        RelativePath = relativePath;
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var dataLength = buffer.ReadVariableInt32();
        Data = buffer.ReadBytes(dataLength);
        // What is the second argument here?
        RelativePath = new ResPath(buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
        buffer.Write(RelativePath.ToString());
        buffer.Write(ResPath.Separator);
    }
}
