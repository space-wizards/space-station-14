using Content.Shared.Lathe;
using Content.Shared.Research;
using Robust.Client.GameObjects;

namespace Content.Client.Research.UI
{
    public sealed class DiskConsoleBoundUserInterface : BoundUserInterface
    {
        private DiskConsoleMenu? _menu;

        public DiskConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {

        }

        protected override void Open()
        {
            base.Open();

            _menu = new();

            _menu.OnClose += Close;
            _menu.OpenCentered();

            _menu.OnServerButtonPressed += () =>
            {
                SendMessage(new LatheServerSelectionMessage());
            };
            _menu.OnPrintButtonPressed += () =>
            {
                SendMessage(new DiskConsolePrintDiskMessage());
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            _menu?.Close();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not DiskConsoleBoundUserInterfaceState msg)
                return;

            _menu?.Update(msg);
        }
    }
}
