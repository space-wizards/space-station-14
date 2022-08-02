using Content.Shared.Body.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Body.UI
{
    [UsedImplicitly]
    public sealed class BodyScannerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private BodyScannerDisplay? _display;

        public BodyScannerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();
            _display = new BodyScannerDisplay(this);
            _display.OnClose += Close;
            _display.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not BodyScannerUIState scannerState)
                return;

            _display?.UpdateDisplay(scannerState);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _display?.Dispose();
            }
        }
    }
}
