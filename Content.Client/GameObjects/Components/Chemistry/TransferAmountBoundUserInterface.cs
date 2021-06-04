using System;
using Content.Client.Eui;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Observer;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Chemistry
{
    [UsedImplicitly]
    public class TransferAmountBoundUserInterface : BoundUserInterface
    {
        private TransferAmountWindow? _window;

        protected override void Open()
        {
            base.Open();
            _window = new TransferAmountWindow();

            _window.applyButton.OnPressed += _ =>
            {
                if (int.TryParse(_window.amountLineEdit.Text, out var i))
                {
                    SendMessage(new TransferAmountSetValueMessage(ReagentUnit.New(i)));
                    _window.Close();
                }
            };

            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {

        }

        public TransferAmountBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }
    }
}
