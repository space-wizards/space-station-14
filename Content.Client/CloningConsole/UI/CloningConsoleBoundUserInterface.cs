using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.Cloning.CloningConsole;

namespace Content.Client.CloningConsole.UI
{
    [UsedImplicitly]
    public sealed class CloningConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private CloningConsoleWindow? _window;

        public CloningConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _window = new CloningConsoleWindow
            {
                Title = Loc.GetString("cloning-console-window-title")
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

            if (_window != null)
            {
                _window.OnClose -= Close;
                _window.CloneButton.OnPressed -= _ => SendMessage(new UiButtonPressedMessage(UiButton.Clone));
            }
            _window?.Dispose();
        }
    }
}
