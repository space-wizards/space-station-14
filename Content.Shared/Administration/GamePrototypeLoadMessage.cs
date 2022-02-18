using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Administration;

public sealed class GamePrototypeLoadMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public string PrototypeData { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
        PrototypeData = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(PrototypeData);
    }
}
