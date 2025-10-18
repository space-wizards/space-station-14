using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.Cloning.CloningConsole;
using Robust.Client.UserInterface;

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

            _window = this.CreateWindow<CloningConsoleWindow>();
            _window.Title = Loc.GetString("cloning-console-window-title");

            _window.CloneButton.OnPressed += _ => SendMessage(new UiButtonPressedMessage(UiButton.Clone));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _window?.Populate((CloningConsoleBoundUserInterfaceState) state);
        }
    }
}
