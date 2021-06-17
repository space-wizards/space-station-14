#nullable enable
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.GameWindow
{
    public sealed class MsgRequestWindowAttention : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            // Nothing
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            // Nothing
        }
    }
}
