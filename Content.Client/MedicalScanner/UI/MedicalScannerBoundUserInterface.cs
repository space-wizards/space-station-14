using JetBrains.Annotations;
using Robust.Client.GameObjects;

using static Content.Shared.MedicalScanner.SharedMedicalScannerComponent;

namespace Content.Client.MedicalScanner.UI
{
    [UsedImplicitly]
    public sealed class MedicalScannerBoundUserInterface : BoundUserInterface
    {
        private MedicalScannerWindow? _window;

        public MedicalScannerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new MedicalScannerWindow
            {
                Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
            };
            _window.OnClose += Close;
            _window.ScanButton.OnPressed += _ => SendMessage(new ScanButtonPressedMessage());
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not MedicalScannerBoundUserInterfaceState cast)
                return;

            _window?.Populate(cast);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_window != null)
                _window.OnClose -= Close;

            _window?.Dispose();
        }
    }
}
