using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.Medical.Components.SharedHealthAnalyzerComponent;

namespace Content.Client.Medical.UI
{
    public class HealthAnalyzerBoundUserInterface : BoundUserInterface
    {
        public HealthAnalyzerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private HealthAnalyzerWindow? _menu;

        protected override void Open()
        {
            base.Open();

            _menu = new HealthAnalyzerWindow(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _menu?.Populate((HealthAnalyzerBoundUserInterfaceState) state);
        }

        public void Refresh()
        {
            SendMessage(new HealthAnalyzerRefreshMessage());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) _menu?.Dispose();
        }
    }
}
