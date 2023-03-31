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
    public ResourcePath RelativePath { get; set; } = ResourcePath.Self;

    public NetworkResourceUploadMessage()
    {
    }

    public NetworkResourceUploadMessage(byte[] data, ResourcePath relativePath)
    {
        Data = data;
        RelativePath = relativePath;
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var dataLength = buffer.ReadVariableInt32();
        Data = buffer.ReadBytes(dataLength);
        RelativePath = new ResourcePath(buffer.ReadString(), buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Data.Length);
        buffer.Write(Data);
        buffer.Write(RelativePath.ToString());
        buffer.Write(RelativePath.Separator);
    }
}
