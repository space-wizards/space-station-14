using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.CloningConsole.SharedCloningConsoleComponent;

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
