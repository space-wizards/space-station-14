using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Forensics;
using Robust.Client.UserInterface;

namespace Content.Client.Forensics
{
    public sealed class ForensicScannerBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

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
            SendPredictedMessage(new ForensicScannerPrintMessage());

            if (_window != null)
                _window.UpdatePrinterState(true);
        }

        private void Clear()
        {
            SendPredictedMessage(new ForensicScannerClearMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null)
                return;

            if (state is not ForensicScannerBoundUserInterfaceState cast)
                return;

            _printCooldown = cast.PrintCooldown;
            _window.UpdateState(cast);
        }
    }
}
