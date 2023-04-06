using Content.Shared.NukeOps;
using Robust.Client.GameObjects;
using System.Threading;
using Content.Client.Stylesheets;
using Timer = Robust.Shared.Timing.Timer;
using Robust.Shared.Timing;
using Content.Client.GameTicking.Managers;

namespace Content.Client.NukeOps
{
    /// <summary>
    /// War declarator that used in NukeOps game rule for declaring war
    /// </summary>
    public sealed class WarDeclaratorBoundUserInterface : BoundUserInterface
    {
        
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private WarDeclaratorWindow? _window;
        public WarDeclaratorBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) {}

        protected override void Open()
        {
            base.Open();

            _window = new WarDeclaratorWindow();
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnMessageEntered += OnMessageChanged;
            _window.OnWarButtonPressed += OnWarButtonPressed;
        }

        private void OnMessageChanged(string newMsg)
        {
            SendMessage(new WarDeclaratorChangedMessage(newMsg));
        }

        private void OnWarButtonPressed()
        {
            SendMessage(new WarDeclaratorPressedWarButton(_window?.MessageLineEdit.Text));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not WarDeclaratorBoundUserInterfaceState cast)
                return;

            _window?.UpdateStatus(cast);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
