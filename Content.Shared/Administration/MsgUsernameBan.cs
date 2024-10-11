using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

public sealed class MsgUsernameBans : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    // special values of ID: -1 clear all
    // add [true] remove [false], id, expression, message
    public List<(bool, bool, int, string, string)> UsernameBans = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        UsernameBans.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            bool add = buffer.ReadBoolean();
            bool extendToBan = buffer.ReadBoolean();
            int id = buffer.ReadInt32();
            string expression = buffer.ReadString();
            string message = buffer.ReadString();

            UsernameBans.Add((add, extendToBan, id, expression, message));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(UsernameBans.Count);

        foreach (var ban in UsernameBans)
        {
            (bool add, bool extendToBan, int id, string expression, string message) = ban;

            buffer.Write(add);
            buffer.Write(extendToBan);
            buffer.Write(id);
            buffer.Write(expression);
            buffer.Write(message);
        }
    }
}
