using System;
using System.Collections.Generic;
using Content.Client.Interfaces.Chat;
using Content.Shared.Administration;
using Content.Shared.Administration.Tickets;
using Content.Shared.Interfaces;
using Robust.Client.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.Administration
{
    public class ClientTicketManager : ITicketManager
    {
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        //[Dependency] private readonly IAdminManager _adminManager = default!;

        public Ticket? CurrentTicket { get; set; }

        Dictionary<int, Ticket> Tickets = new();

        public bool HasTicket(NetUserId _)
        {
            if (CurrentTicket != null)
            {
                return true;
            }
            return false;
        }

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgTicketMessage>(MsgTicketMessage.NAME, OnTicketMessage);
            _netManager.RegisterNetMessage<MsgViewTicket>(MsgViewTicket.NAME);
        }

        public void OnTicketMessage(MsgTicketMessage message)
        {
            return;
        }

        public void CreateTicket(NetUserId opener, NetUserId target, string message)
        {
            return;
        }

        public Ticket? GetTicket(int id)
        {
            return CurrentTicket;
        }
    }
}
