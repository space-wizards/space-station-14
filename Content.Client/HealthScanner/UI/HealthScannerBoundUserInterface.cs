using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.HealthScanner.SharedHealthScannerComponent;

namespace Content.Client.HealthScanner.UI
{
    [UsedImplicitly]
    public class HealthScannerBoundUserInterface : BoundUserInterface
    {
        private HealthScannerWindow? _window;

        public HealthScannerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new HealthComponentSyncRequestMessage());
        }

        protected override void Open()
        {
            base.Open();
            _window = new HealthScannerWindow
            {
                Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
            };
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null)
                return;

            switch (message)
            {
                case HealthComponentDamageMessage addrMsg:
                    _window?.Populate(addrMsg);
                    break;
            }
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
