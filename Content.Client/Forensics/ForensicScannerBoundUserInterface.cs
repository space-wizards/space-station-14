using Content.Shared.Forensics;
using Robust.Client.GameObjects;

namespace Content.Client.Forensics
{
    public sealed class ForensicScannerBoundUserInterface : BoundUserInterface
    {
        private ForensicScannerMenu? _window;

        public ForensicScannerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new ForensicScannerMenu();
            _window.OnClose += Close;
            _window.Print.OnPressed += _ => Print();
            _window.Clear.OnPressed += _ => Clear();
            _window.OpenCentered();
        }

        private void Print()
        {
            SendMessage(new ForensicScannerPrintMessage());
        }

        private void Clear()
        {
            SendMessage(new ForensicScannerClearMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null)
                return;

            if (state is not ForensicScannerBoundUserInterfaceState cast)
                return;

            _window.UpdateState(cast);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _window?.Dispose();
        }
    }
}
