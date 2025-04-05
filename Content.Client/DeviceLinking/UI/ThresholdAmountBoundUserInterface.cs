using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.DeviceLinking.UI
{
    [UsedImplicitly]
    public sealed class ThresholdAmountBoundUserInterface : BoundUserInterface
    {
        private IEntityManager _entManager;
        private EntityUid _owner;
        [ViewVariables]
        private ThresholdAmountWindow? _window;

        public ThresholdAmountBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
          _owner = owner;
          _entManager = IoCManager.Resolve<IEntityManager>();
        }

        protected override void Open()
        {
            base.Open();
            _window = this.CreateWindow<ThresholdAmountWindow>();

            if (_entManager.TryGetComponent<PowerThresholdComponent>(_owner, out var comp))
                _window.SetBounds(comp.MinimumThresholdAmount.Int(), comp.MaximumThresholdAmount.Int());

            _window.ApplyButton.OnPressed += _ =>
            {
                if (int.TryParse(_window.AmountLineEdit.Text, out var i))
                {
                    SendMessage(new ThresholdAmountSetValueMessage(FixedPoint2.New(i)));
                    _window.Close();
                }
            };
        }
    }
}
