using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Administration.Menu
{
    public class AdminMenuPlayerListRequest : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
