using Content.Shared.Research;
using Content.Shared.Research.Components;
using Robust.Client.GameObjects;
using BoundUserInterface = Robust.Shared.GameObjects.BoundUserInterface;

namespace Content.Client.Research.UI
{
    public sealed class DiskConsoleBoundUserInterface : Robust.Shared.GameObjects.BoundUserInterface
    {
        [ViewVariables]
        private DiskConsoleMenu? _menu;

        public DiskConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
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
                SendMessage(new ConsoleServerSelectionMessage());
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
