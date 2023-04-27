using Robust.Client.GameObjects;
using Content.Shared.Clothing;

namespace Content.Client.Clothing.UI
{
    /// <summary>
    /// Initializes a <see cref="NorthStarWindow"/> and updates it when new server messages are received.
    /// </summary>
    public sealed class NorthStarBoundUserInterface : BoundUserInterface
    {
        private NorthStarWindow? _window;

        public NorthStarBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new NorthStarWindow();
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnBattlecryEntered += OnBattlecryChanged;
        }


        private void OnBattlecryChanged(string newBattlecry)
        {
            SendMessage(new MeleeSpeechBattlecryChangedMessage(newBattlecry));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not MeleeSpeechBoundUserInterfaceState cast)
                return;

            _window.SetCurrentBattlecry(cast.CurrentBattlecry);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
