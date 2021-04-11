using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Shared.Administration;
using Content.Shared.Administration.Tickets;
using Content.Shared.Chat;
using Content.Shared.Interfaces;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.Server.Administration
{
    public class TicketManager : ITicketManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;

        public const int MaxMessageLength = 1024;

        public int CurrentId = 0;

        Dictionary<int, Ticket> Tickets = new();

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgTicketMessage>(MsgTicketMessage.NAME, OnTicketMessage);
        }

        public bool HasTicket(NetUserId id)
        {
            foreach (var (_, ticket) in Tickets)
            {
                if (ticket.TicketOpen && id == ticket.TargetPlayer)
                {
                    return true;
                }
            }
            return false;
        }

        public void CreateTicket(NetUserId opener, NetUserId? target, string message)
        {
            var ticket = new Ticket(CurrentId, opener, target, message);
            Tickets.Add(CurrentId, ticket);
            CurrentId++;
            var player = _playerManager.GetSessionByUserId(opener);
            var msg = message.Length <= 32 ? message.Trim() : $"{message.Trim().Substring(0, 32)}...";
            _chatManager.SendAdminAnnouncement(
                $"Ticket {ticket.Id} opened by {player.ConnectedClient.UserName}: {msg}");
        }

        public void OnTicketMessage(MsgTicketMessage message)
        {
            switch (message.Action)
            {
                case TicketAction.PlayerOpen:
                    if (HasTicket(message.TargetPlayer))
                    {
                        return;
                    }

                    CreateTicket(message.TargetPlayer, null, message.Message);
                    break;
            }
        }

        public int CountTickets(bool onlyOpen)
        {
            if (!onlyOpen)
            {
                return Tickets.Count;
            }

            int count = 0;
            foreach (var (_, ticket) in Tickets)
            {
                if (ticket.TicketOpen)
                {
                    count++;
                }
            }
            return count;
        }

        private void SendTicketToAdmins(Ticket ticket)
        {
            var clients = _adminManager.ActiveAdmins.Select(p => p.ConnectedClient);

            var msg = _netManager.CreateNetMessage<MsgTicketMessage>();

            msg.Id = ticket.Id;
            msg.Message = Ticket.Messages. <= 32 ? message.Trim() : $"{message.Trim().Substring(0, 32)}...";;
            msg.MessageWrap = $"{Loc.GetString("ADMIN")}: {{0}}";

            _netManager.ServerSendToMany(msg, clients.ToList());
        }
    }
}
