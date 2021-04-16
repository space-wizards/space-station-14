using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Administration.Tickets;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Player;
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

            _window.CloseTicketButton.OnPressed += _ =>
            {
                //SendMessage(new AcceptCloningChoiceMessage(AcceptCloningUiButton.Deny));
                _window.Close();
            };

            _window.ResolveTicketButton.OnPressed += _ =>
            {
                //SendMessage(new AcceptCloningChoiceMessage(AcceptCloningUiButton.Accept));
                _window.Close();
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

    }
}
