using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Players.JobWhitelist;

public sealed class MsgJobWhitelist : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public HashSet<string> Whitelist = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        Whitelist.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            Whitelist.Add(buffer.ReadString());
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Whitelist.Count);

        foreach (var ban in Whitelist)
        {
            buffer.Write(ban);
        }
    }
}
