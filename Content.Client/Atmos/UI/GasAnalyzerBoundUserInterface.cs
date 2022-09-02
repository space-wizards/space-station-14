using Robust.Client.GameObjects;
using static Content.Shared.Atmos.Components.SharedGasAnalyzerComponent;

namespace Content.Client.Atmos.UI
{
    public sealed class GasAnalyzerBoundUserInterface : BoundUserInterface
    {
        public GasAnalyzerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        private GasAnalyzerWindow? _window;

        protected override void Open()
        {
            base.Open();

            _window = new GasAnalyzerWindow(this);
            _window.OnClose += OnClose;
            _window.OpenCentered();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null)
                return;
            if (message is not GasAnalyzerUserMessage cast)
                return;
            _window.Populate(cast);
        }

        /// <summary>
        /// Closes UI and tells the server to disable the analyzer
        /// </summary>
        private void OnClose()
        {
            SendMessage(new GasAnalyzerDisableMessage());
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _window?.Dispose();
        }
    }
}
