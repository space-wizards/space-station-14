using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

public sealed class MsgUsernameBan : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    // special values of ID: -1 [remove] clear all
    // add [true] remove [false], id, expression, message
    public MsgUsernameBanContent UsernameBan = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        int id = buffer.ReadInt32();
        bool add = buffer.ReadBoolean();
        bool regex = buffer.ReadBoolean();
        bool extendToBan = buffer.ReadBoolean();
        buffer.ReadPadBits();
        string expression = buffer.ReadString();

        UsernameBan = new(id, add, regex, extendToBan, expression);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(UsernameBan.Id);
        buffer.Write(UsernameBan.Add);
        buffer.Write(UsernameBan.Regex);
        buffer.Write(UsernameBan.ExtendToBan);
        buffer.WritePadBits();
        buffer.Write(UsernameBan.Expression);
    }
}

public readonly record struct MsgUsernameBanContent(
    int Id,
    bool Add,
    bool Regex,
    bool ExtendToBan,
    string Expression
);
