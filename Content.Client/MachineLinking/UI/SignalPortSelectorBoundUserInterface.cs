using Content.Shared.MachineLinking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.MachineLinking.UI
{
    public sealed class SignalPortSelectorBoundUserInterface : BoundUserInterface
    {
        private SignalPortSelectorMenu? _menu;

        public SignalPortSelectorBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new SignalPortSelectorMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            switch (state)
            {
                case SignalPortsState data:
                    _menu?.UpdateState(data);
                    break;
            }
        }

        public void OnPortSelected(string port)
        {
            SendMessage(new SignalPortSelected(port));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
