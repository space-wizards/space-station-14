using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using static Content.Shared.Atmos.Components.GasAnalyzerComponent;

namespace Content.Client.Atmos.UI
{
    public sealed class GasAnalyzerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private GasAnalyzerWindow? _window;

        public GasAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindowCenteredLeft<GasAnalyzerWindow>();
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
