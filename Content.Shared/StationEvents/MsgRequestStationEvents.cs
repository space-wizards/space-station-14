#nullable enable
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.StationEvents
{
    public class MsgRequestStationEvents : NetMessage
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
