using System.Collections.Generic;
using System.Linq;
using Content.Server.Eui;
using Content.Server.GameTicking;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminMenu;
using Content.Shared.Administration.Tickets;
using Content.Shared.Chat;
using Content.Shared.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;

namespace Content.Server.Administration
{
    public class TicketManager : ITicketManager
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;

        public const int MaxMessageLength = 1024;

        public int CurrentId = 0;

        Dictionary<int, Ticket> Tickets = new();

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgTicketMessage>(MsgTicketMessage.NAME, OnTicketMessage);
            _netManager.RegisterNetMessage<MsgViewTicket>(MsgViewTicket.NAME, ViewTicket);
            _netManager.RegisterNetMessage<AdminMenuTicketListRequest>(AdminMenuTicketListRequest.NAME, HandleTicketListRequest);
            _netManager.RegisterNetMessage<AdminMenuTicketListMessage>(AdminMenuTicketListMessage.NAME);
            _gameTicker.OnRunLevelChanged += RunLevelChanged;
        }

        private void RunLevelChanged(GameRunLevelChangedEventArgs level)
        {
            if (level.NewRunLevel == GameRunLevel.PreRoundLobby)
            {
                CurrentId = 0;
                Tickets.Clear();
            }
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

        public void CreateTicket(NetUserId opener, NetUserId target, string message)
        {
            var ticket = new Ticket(CurrentId, opener, target, message);
            Tickets.Add(CurrentId, ticket);
            CurrentId++;
            var player = _playerManager.TryGetSessionById(opener, out var session);
            var mind = session?.ContentData()?.Mind;
            var character = mind == null ? "Unknown" : mind.CharacterName;
            ticket.Character = character;
            var username = session?.ConnectedClient.UserName ?? string.Empty;
            ticket.PlayerUsername = username;
            var msg = message.Length <= 48 ? message.Trim() : $"{message.Trim().Substring(0, 48)}...";
            _chatManager.SendAdminAnnouncement(
                $"Ticket {ticket.Id} opened by {username} ({character}): {msg}");
        }

        public Ticket? GetTicket(int id)
        {
            return Tickets[id];
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

                    CreateTicket(message.TargetPlayer, message.TargetPlayer, message.Message);
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

        public void ViewTicket(MsgViewTicket message)
        {
            var session = _playerManager.GetSessionByChannel(message.MsgChannel);
            var eui = IoCManager.Resolve<EuiManager>();
            var ui = new TicketEui();
            eui.OpenEui(ui, session);
            ui.TicketId = message.TicketId;
            ui.StateDirty();
        }

        private void HandleTicketListRequest(AdminMenuTicketListRequest message)
        {
            var senderSession = _playerManager.GetSessionByChannel(message.MsgChannel);

            if (!_adminManager.IsAdmin(senderSession))
            {
                return;
            }

            var netMsg = _netManager.CreateNetMessage<AdminMenuTicketListMessage>();
            netMsg.TicketsInfo.Clear();

            foreach (var (_, ticket) in Tickets)
            {
                var id = ticket.Id;
                var player = string.IsNullOrEmpty(ticket.PlayerUsername) ? ticket.TargetPlayer.ToString() : ticket.PlayerUsername;
                var name = $"{player} ({ticket.Character})";
                var admin = ticket.ClaimedAdmin.ToString() ?? string.Empty;
                var status = ticket.Status;
                var msg = ticket.Messages.First();
                var summary = msg.message.Length <= 48 ? msg.message.Trim() : $"{msg.message.Trim().Substring(0, 48)}...";

                netMsg.TicketsInfo.Add(new AdminMenuTicketListMessage.TicketInfo(id, name, admin, status, summary));
            }

            _netManager.ServerSendMessage(netMsg, senderSession.ConnectedClient);
        }
    }
}
