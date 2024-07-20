using Content.Shared.Chemistry;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI
{
    [UsedImplicitly]
    public sealed class TransferAmountBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private TransferAmountWindow? _window;

        public TransferAmountBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = this.CreateWindow<TransferAmountWindow>();

            _window.ApplyButton.OnPressed += _ =>
            {
                if (int.TryParse(_window.AmountLineEdit.Text, out var i))
                {
                    SendMessage(new TransferAmountSetValueMessage(FixedPoint2.New(i)));
                    _window.Close();
                }
            };
        }
    }
}
