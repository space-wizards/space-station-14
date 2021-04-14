using Content.Client.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration
{
    [UsedImplicitly]
    public class TicketEui : BaseEui
    {
        private readonly TicketWindow _window;

        public TicketEui()
        {
            _window = new TicketWindow();

            _window.CloseButton.OnPressed += _ =>
            {
                //SendMessage(new AcceptCloningChoiceMessage(AcceptCloningUiButton.Deny));
                _window.Close();
            };

            _window.ResolveButton.OnPressed += _ =>
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

    }
}
