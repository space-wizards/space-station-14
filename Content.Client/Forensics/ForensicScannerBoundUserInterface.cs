using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Forensics;

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
            _window = new ForensicScannerMenu();
            _window.OnClose += Close;
            _window.Print.OnPressed += _ => Print();
            _window.Clear.OnPressed += _ => Clear();
            _window.OpenCentered();
        }

        private void Print()
        {
            SendMessage(new ForensicScannerPrintMessage());

            if (_window != null)
                _window.UpdatePrinterState(true);

            // This UI does not require pinpoint accuracy as to when the Print
            // button is available again, so spawning client-side timers is
            // fine. The server will make sure the cooldown is honored.
            Timer.Spawn(_printCooldown, () =>
            {
                if (_window != null)
                    _window.UpdatePrinterState(false);
            });
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

            if (cast.PrintReadyAt > _gameTiming.CurTime)
                Timer.Spawn(cast.PrintReadyAt - _gameTiming.CurTime, () =>
                {
                    if (_window != null)
                        _window.UpdatePrinterState(false);
                });

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
