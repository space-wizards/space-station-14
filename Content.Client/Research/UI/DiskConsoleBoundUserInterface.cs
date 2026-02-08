using Content.Shared.Research;
using Content.Shared.Research.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Research.UI
{
    public sealed class DiskConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DiskConsoleMenu? _menu;

        public DiskConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<DiskConsoleMenu>();

            _menu.OnServerButtonPressed += () =>
            {
                SendMessage(new ConsoleServerSelectionMessage());
            };
            _menu.OnPrintButtonPressed += () =>
            {
                SendMessage(new DiskConsolePrintDiskMessage());
            };
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
