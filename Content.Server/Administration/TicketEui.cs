using System;
using Content.Server.Eui;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Administration.Tickets;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.Administration
{
    public class TicketEui : BaseEui
    {
        [Dependency] private readonly ITicketManager _ticketManager = default!;

        public NetUserId Owner;
        public int TicketId;
        public override TicketEuiState GetNewState()
        {
            var ticket = _ticketManager.GetTicket(TicketId);
            var state = new TicketEuiState(ticket);
            return state;
        }

        public TicketEui(NetUserId owner)
        {
            IoCManager.InjectDependencies(this);
            Owner = owner;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);
            switch (msg)
            {
                case TicketsEuiMsg.TicketSendMessage message:
                {
                    var text = message.Message.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _ticketManager.NewMessage(TicketId, Owner, text);
                    }
                    break;
                }
                case TicketsEuiMsg.TicketChangeStatus message:
                {
                    _ticketManager.ChangeStatus(TicketId, Owner, message.Status);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
