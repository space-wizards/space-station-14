using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public sealed class NetworkResourceUploadMessage : NetMessage
{
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public byte[] Data { get; set; } = Array.Empty<byte>();
    public ResourcePath RelativePath { get; set; } = ResourcePath.Self;

    public NetworkResourceUploadMessage()
    {
    }

    public NetworkResourceUploadMessage(byte[] data, ResourcePath relativePath)
    {
        Data = data;
        RelativePath = relativePath;
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        var dataLength = buffer.ReadVariableInt32();
        Data = buffer.ReadBytes(dataLength);
        RelativePath = new ResourcePath(buffer.ReadString(), buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
        buffer.Write(RelativePath.ToString());
        buffer.Write(RelativePath.Separator);
    }
}
