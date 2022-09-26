using JetBrains.Annotations;
using Robust.Client.GameObjects;

using static Content.Shared.MedicalScanner.SharedMedicalResearchBedComponent;

namespace Content.Client.MedicalResearchBed.UI
{
    [UsedImplicitly]
    public sealed class MedicalResearchBedBoundUserInterface : BoundUserInterface
    {
        private MedicalResearchBedWindow? _window;

        public MedicalResearchBedBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new MedicalResearchBedWindow
            {
                Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
            };
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null)
                return;

            if (message is not MedicalResearchBedScannedUserMessage cast)
                return;

            _window.Populate(cast);
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
