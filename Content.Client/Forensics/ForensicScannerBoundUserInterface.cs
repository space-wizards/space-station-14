using Robust.Shared.Timing;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Forensics
{
    public sealed class ForensicScannerBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables]
        private ForensicScannerMenu? _window;

        public ForensicScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = this.CreateWindow<ForensicScannerMenu>();
            _window.Print.OnPressed += _ => Print();
            _window.Clear.OnPressed += _ => Clear();

            Update();
        }

        private void Print()
        {
            SendPredictedMessage(new ForensicScannerPrintMessage());
        }

        private void Clear()
        {
            SendPredictedMessage(new ForensicScannerClearMessage());
        }

        public override void Update()
        {
            base.Update();

            if (_window == null)
                return;

            if (!EntMan.TryGetComponent(Owner, out ForensicScannerComponent? scanner))
                return;

            _window.Update(scanner);
        }
    }
}
