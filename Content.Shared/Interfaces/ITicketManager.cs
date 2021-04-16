using System.Collections.Generic;
using Content.Shared.Administration;
using Content.Shared.Administration.Tickets;
using Robust.Shared.Network;

namespace Content.Shared.Interfaces
{
    public interface ITicketManager
    {
        void Initialize();

        void CreateTicket(NetUserId opener, NetUserId target, string message);

        void OnTicketMessage(MsgTicketMessage message);

        bool HasTicket(NetUserId id);

        Ticket? GetTicket(int id);

        void NewMessage(int id, NetUserId author, string message);
    }
}
