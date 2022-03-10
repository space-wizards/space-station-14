using Content.Shared.Speech.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Speech.UI
{
    /// <summary>
    /// Initializes a <see cref="VoiceChangerWindow"/> and updates it when new server messages are received.
    /// </summary>
    public sealed class VoiceChangerBoundUserInterface : BoundUserInterface
    {
        private VoiceChangerWindow? _window;

        public VoiceChangerBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new VoiceChangerWindow();
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnNameEntered += OnNameChanged;
        }

        private void OnNameChanged(string newName)
        {
            SendMessage(new VoiceChangerNameChangedMessage(newName));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not VoiceChangerBoundUserInterfaceState cast)
                return;

            _window.SetCurrentName(cast.CurrentName);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
