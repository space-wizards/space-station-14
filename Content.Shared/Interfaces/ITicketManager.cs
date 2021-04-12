using Content.Shared.Administration;
using Robust.Shared.Network;

namespace Content.Shared.Interfaces
{
    public interface ITicketManager
    {
        void Initialize();

        void CreateTicket(NetUserId opener, NetUserId target, string message);

        void OnTicketMessage(MsgTicketMessage message);

        bool HasTicket(NetUserId id);
    }
}
