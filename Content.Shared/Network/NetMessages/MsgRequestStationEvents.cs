using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.NetMessages
{
    public class MsgRequestStationEvents : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgRequestStationEvents);
        public MsgRequestStationEvents(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
