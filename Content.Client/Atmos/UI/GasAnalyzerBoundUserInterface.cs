using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Atmos.Components.SharedGasAnalyzerComponent;

namespace Content.Client.Atmos.UI
{
    public class GasAnalyzerBoundUserInterface : BoundUserInterface
    {
        public GasAnalyzerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private GasAnalyzerWindow? _window;

        protected override void Open()
        {
            base.Open();

            _window = new GasAnalyzerWindow();
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            var castState = (GasAnalyzerBoundUserInterfaceState) state;
            _window?.UpdateState(castState);
        }

        public void Refresh()
        {
            SendMessage(new GasAnalyzerRefreshMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) _window?.Dispose();
        }
    }
}
