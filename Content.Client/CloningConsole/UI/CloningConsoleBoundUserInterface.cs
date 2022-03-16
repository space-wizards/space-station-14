using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.Cloning.CloningConsole;

namespace Content.Client.CloningConsole.UI
{
    [UsedImplicitly]
    public sealed class CloningConsoleBoundUserInterface : BoundUserInterface
    {
        private CloningConsoleWindow? _window;

        public CloningConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new CloningConsoleWindow
            {
                Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Owner).EntityName,
            };
            _window.OnClose += Close;
            _window.CloneButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.Clone));
            _window.EjectButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.Eject));
            _window.GeneticScannerRefreshButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.Refresh));
            _window.CloningPodRefreshButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.Refresh));
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _window?.Populate((CloningConsoleBoundUserInterfaceState) state);
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
