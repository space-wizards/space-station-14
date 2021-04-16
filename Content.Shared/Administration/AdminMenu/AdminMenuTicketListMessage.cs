#nullable enable
using System.Collections.Generic;
using Content.Shared.Administration.Tickets;
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Administration.AdminMenu
{
    public class AdminMenuTicketListMessage : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Command;
        public static readonly string NAME = nameof(AdminMenuTicketListMessage);
        public AdminMenuTicketListMessage(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public List<TicketInfo> TicketsInfo = new();

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var count = buffer.ReadInt32();

            TicketsInfo.Clear();

            for (var i = 0; i < count; i++)
            {
                var id = buffer.ReadInt32();
                var username = buffer.ReadString();
                var admin = buffer.ReadString();
                var status = (TicketStatus)buffer.ReadByte();
                var message = buffer.ReadString();

                TicketsInfo.Add(new TicketInfo(id, username, admin, status, message));
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(TicketsInfo.Count);

            foreach (var ticket in TicketsInfo)
            {
                buffer.Write(ticket.Id);
                buffer.Write(ticket.Name);
                buffer.Write(ticket.Admin);
                buffer.Write((byte)ticket.Status);
                buffer.Write(ticket.Message);
            }
        }

        public record TicketInfo(int Id, string Name, string Admin, TicketStatus Status, string Message);
    }
}
