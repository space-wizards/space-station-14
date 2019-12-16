using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;
using static Content.Shared.GameObjects.Components.Medical.SharedMedicalScannerComponent;

namespace Content.Client.GameObjects.Components.MedicalScanner
{
    public class MedicalScannerBoundUserInterface : BoundUserInterface
    {
        public MedicalScannerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        private MedicalScannerWindow _window;

        protected override void Open()
        {
            base.Open();
            _window = new MedicalScannerWindow
            {
                Title = Owner.Owner.Name,
            };
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window.Populate((MedicalScannerBoundUserInterfaceState) state);
        }
    }
}
