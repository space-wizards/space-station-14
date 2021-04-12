using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Administration.AdminMenu
{
    public class AdminMenuTicketListRequest : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Command;
        public static readonly string NAME = nameof(AdminMenuTicketListRequest);
        public AdminMenuTicketListRequest(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
