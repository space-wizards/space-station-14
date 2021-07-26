using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Chemistry.UI
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

        public TransferAmountBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }
    }
}
