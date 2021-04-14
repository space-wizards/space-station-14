using Lidgren.Network;
using Robust.Shared.Network;
using JetBrains.Annotations;

#nullable disable

namespace Content.Shared.Administration.Tickets
{
    public class MsgViewTicket : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Core;
        public static readonly string NAME = nameof(MsgViewTicket);
        public MsgViewTicket(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public int TicketId { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            TicketId = buffer.ReadInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(TicketId);
        }
    }
}
