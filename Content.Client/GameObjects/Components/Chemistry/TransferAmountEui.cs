using Content.Client.Eui;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Observer;

namespace Content.Client.GameObjects.Components.Chemistry
{
    public class TransferAmountEui : BaseEui
    {
        private readonly TransferAmountWindow _window;

        public TransferAmountEui()
        {
            _window = new TransferAmountWindow();

            _window.applyButton.OnPressed += _ =>
            {
                SendMessage(new TransferAmountEuiMessage((int)_window.amountSlider.Value));
                _window.Close();
            };
        }

        public override void Opened()
        {
            _window.Open();
        }

        public override void Closed()
        {
            _window.Close();
        }
    }
}
