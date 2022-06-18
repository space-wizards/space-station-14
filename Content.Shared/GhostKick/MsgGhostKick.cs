using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.GhostKick;

public sealed class MsgGhostKick : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public override void ReadFromBuffer(NetIncomingMessage buffer)
    {
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer)
    {
    }
}
