using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

public sealed class MsgDeleteChatMessagesBy : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int Key;
    public HashSet<NetEntity> Entities = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Key = buffer.ReadInt32();

        var entities = buffer.ReadInt32();
        Entities = new HashSet<NetEntity>(entities);

        for (var i = 0; i < entities; i++)
        {
            Entities.Add(buffer.ReadNetEntity());
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Key);

        buffer.Write(Entities.Count);
        foreach (var ent in Entities)
        {
            buffer.Write(ent);
        }
    }
}
