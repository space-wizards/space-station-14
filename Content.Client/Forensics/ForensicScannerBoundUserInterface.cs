using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Forensics;
using Robust.Client.UserInterface;

namespace Content.Client.Forensics
{
    public sealed partial class ForensicScannerBoundUserInterface : BoundUserInterface
    {
        private static readonly EntityTimerId PrintTimer = new("print");

        [ViewVariables]
        private ForensicScannerMenu? _window;

        [ViewVariables]
        private TimeSpan _printCooldown;

        public ForensicScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = this.CreateWindow<ForensicScannerMenu>();
            _window.Print.OnPressed += _ => Print();
            _window.Clear.OnPressed += _ => Clear();
        }

        private void Print()
        {
            SendMessage(new ForensicScannerPrintMessage());

            if (_window != null)
                _window.UpdatePrinterState(true);

            SetTimer(PrintTimer, _printCooldown);
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

            _printCooldown = cast.PrintCooldown;

            SetTimerAt(PrintTimer, cast.PrintReadyAt);

            _window.UpdateState(cast);
        }

        protected override void OnTimer(EntityTimerEvent timer)
        {
            if (timer.Id == PrintTimer)
                _window?.UpdatePrinterState(false);
        }
    }
}
