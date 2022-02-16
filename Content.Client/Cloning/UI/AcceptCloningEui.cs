using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;

namespace Content.Client.Cloning.UI
{
    [UsedImplicitly]
    public sealed class AcceptCloningEui : BaseEui
    {
        private readonly AcceptCloningWindow _window;

        public AcceptCloningEui()
        {
            _window = new AcceptCloningWindow();

            _window.DenyButton.OnPressed += _ =>
            {
                SendMessage(new AcceptCloningChoiceMessage(AcceptCloningUiButton.Deny));
                _window.Close();
            };

            _window.AcceptButton.OnPressed += _ =>
            {
                SendMessage(new AcceptCloningChoiceMessage(AcceptCloningUiButton.Accept));
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
