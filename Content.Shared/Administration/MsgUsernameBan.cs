using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

public sealed class MsgUsernameBan : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    // special values of ID: -1 clear all
    // add [true] remove [false], id, expression, message
    public (bool, bool, int, string, string) UsernameBan = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        int id = buffer.ReadInt32();
        bool add = buffer.ReadBoolean();
        bool extendToBan = buffer.ReadBoolean();
        buffer.ReadPadBits();
        string expression = buffer.ReadString();
        string message = buffer.ReadString();

        UsernameBan = (add, extendToBan, id, expression, message);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {

        (bool add, bool extendToBan, int id, string expression, string message) = UsernameBan;

        buffer.Write(id);
        buffer.Write(add);
        buffer.Write(extendToBan);
        buffer.WritePadBits();
        buffer.Write(expression);
        buffer.Write(message);
    }
}
