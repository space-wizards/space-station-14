using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
            _window.ScanButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.ScanDNA));
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _window?.Populate((MedicalScannerBoundUserInterfaceState) state);
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
