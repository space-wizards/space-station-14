using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI
{
    [UsedImplicitly]
    public sealed class TransferAmountBoundUserInterface : BoundUserInterface
    {
        private IEntityManager _entManager;
        private EntityUid _owner;
        [ViewVariables]
        private TransferAmountWindow? _window;

        public TransferAmountBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
          _owner = owner;
          _entManager = IoCManager.Resolve<IEntityManager>();
        }

        protected override void Open()
        {
            base.Open();
            _window = this.CreateWindow<TransferAmountWindow>();

            if (_entManager.TryGetComponent<SolutionTransferComponent>(_owner, out var comp))
                _window.SetBounds(comp.MinimumTransferAmount.Int(), comp.MaximumTransferAmount.Int());

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
