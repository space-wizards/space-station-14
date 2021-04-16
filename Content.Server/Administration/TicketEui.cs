using Content.Server.Eui;
using Content.Server.Players;
using Content.Shared.Administration.Tickets;
using Content.Shared.Eui;
using Content.Shared.GameObjects.Components.Observer;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration
{
    public class TicketEui : BaseEui
    {
        public int TicketId;
        public override TicketEuiState GetNewState()
        {
            var ticketMan = IoCManager.Resolve<ITicketManager>();
            var ticket = ticketMan.GetTicket(TicketId);
            var state = new TicketEuiState(ticket);
            return state;
        }

        /*public TicketEui(Ticket ticket)
        {
            _ticket = ticket;
        }*/

        /*public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AcceptCloningChoiceMessage choice
                || choice.Button == AcceptCloningUiButton.Deny)
            {
                Close();
                return;
            }

            var mind = Player.ContentData()?.Mind;
            //mind?.TransferTo(_newMob);
            mind?.UnVisit();
            Close();
        }*/

    }
}
