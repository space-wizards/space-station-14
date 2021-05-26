using System;
using Content.Client.Eui;
using Content.Shared.Chemistry;
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
                if (int.TryParse(_window.amountLineEdit.Text, out var i))
                {
                    SendMessage(new TransferAmountEuiMessage(ReagentUnit.New(i)));
                    _window.Close();
                }
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
