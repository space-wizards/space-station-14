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
            _window.OpenCentered();
        }

        private void Print()
        {
            SendMessage(new ForensicScannerPrintMessage());
            _window?.Close();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null)
                return;

            if (message is not ForensicScannerUserMessage cast)
                return;

            _window.Populate(cast);
        }
    }
}
