using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Atmos.Components.SharedGasAnalyzerComponent;

namespace Content.Client.Atmos.UI
{
    public sealed class GasAnalyzerBoundUserInterface : BoundUserInterface
    {
        public GasAnalyzerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private GasAnalyzerWindow? _menu;

        protected override void Open()
        {
            base.Open();

            _menu = new GasAnalyzerWindow(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _menu?.Populate((GasAnalyzerBoundUserInterfaceState) state);
        }

        public void Refresh()
        {
            SendMessage(new GasAnalyzerRefreshMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) _menu?.Dispose();
        }
    }
}
