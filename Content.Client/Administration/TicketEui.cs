using System;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Administration.Tickets;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Administration
{
    [UsedImplicitly]
    public class TicketEui : BaseEui
    {
        private readonly TicketWindow _window;

        public TicketEui()
        {
            _window = new TicketWindow();

            _window.MessageSend.OnPressed += _ =>
            {
                var text = _window.MessageInput.Text.Trim();
                if (_window.Ticket is not null && !string.IsNullOrEmpty(text))
                {
                    SendMessage(new TicketsEuiMsg.TicketSendMessage(text));
                    _window.MessageInput.Clear();
                }
            };

            _window.ClaimTicketButton.OnPressed += _ =>
            {
                if (!_window.IsAdmin || _window.Ticket?.Status == TicketStatus.Resolved || _window.Ticket?.Status == TicketStatus.Closed) return;
                SendMessage(new TicketsEuiMsg.TicketChangeStatus(_window.Ticket?.Status == TicketStatus.Unclaimed ? TicketStatus.Claimed : TicketStatus.Unclaimed));
            };

            _window.CloseTicketButton.OnPressed += _ =>
            {
                if (!_window.IsAdmin || _window.Ticket?.Status == TicketStatus.Resolved || _window.Ticket?.Status == TicketStatus.Unclaimed) return;
                SendMessage(new TicketsEuiMsg.TicketChangeStatus(TicketStatus.Closed));
            };

            _window.ResolveTicketButton.OnPressed += _ =>
            {
                if (!_window.IsAdmin || _window.Ticket?.Status == TicketStatus.Closed || _window.Ticket?.Status == TicketStatus.Unclaimed) return;
                SendMessage(new TicketsEuiMsg.TicketChangeStatus(TicketStatus.Resolved));
            };
        }

        public override void Opened()
        {
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not TicketEuiState ticketState || ticketState.ticket is null) return;

            var _playMan = IoCManager.Resolve<IPlayerManager>();
            _window.Session = _playMan.LocalPlayer?.Session;
            var _adminMan = IoCManager.Resolve<IClientAdminManager>();
            _window.IsAdmin = _adminMan.HasFlag(AdminFlags.Admin);

            _window.LoadTicket(ticketState.ticket);

        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);
            switch (msg)
            {
                case TicketsEuiMsg.TicketReceiveMessage message:
                {
                    _window.AddMessage(message.Message);
                    break;
                }

                case TicketsEuiMsg.TicketChangeStatus message:
                {
                    if(_window.Ticket == null) return;

                    switch (message.Status)
                    {
                        case TicketStatus.Claimed:
                        {
                            _window.Ticket.ClaimedAdmin = message.Admin;
                            _window.Ticket.AdminUsername = message.AdminUsername ?? "Unknown";
                            _window.Ticket.Status = TicketStatus.Claimed;
                            //var notify = ServerMessage($"Ticket claimed by {message.AdminUsername}");
                            //_window.Ticket.Messages.Add(notify);
                            //_window.AddMessage(notify);
                            _window.WindowStatus = TicketStatus.Claimed;
                            _window.RefreshButtons();
                            break;
                        }
                        case TicketStatus.Unclaimed:
                        {
                            _window.Ticket.ClaimedAdmin = message.Admin;
                            _window.Ticket.AdminUsername = message.AdminUsername ?? "Unknown";
                            _window.Ticket.Status = TicketStatus.Unclaimed;
                            //var notify = ServerMessage($"Ticket claimed by {message.AdminUsername}");
                            //_window.Ticket.Messages.Add(notify);
                            //_window.AddMessage(notify);
                            _window.WindowStatus = TicketStatus.Unclaimed;
                            _window.RefreshButtons();
                            break;
                        }
                    }
                    break;
                }
            }
        }

        public TicketMessage ServerMessage(string message)
        {
            var time = DateTimeOffset.Now;
            return new TicketMessage(time.Ticks, time.Offset.Ticks, "[Server]", true, message);
        }

    }
}
