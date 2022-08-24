using Content.Shared.Labels;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Labels.UI
{
    /// <summary>
    /// Initializes a <see cref="HandLabelerWindow"/> and updates it when new server messages are received.
    /// </summary>
    public sealed class HandLabelerBoundUserInterface : BoundUserInterface
    {
        private HandLabelerWindow? _window;

        public HandLabelerBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new HandLabelerWindow();
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnLabelEntered += OnLabelChanged;

        }

        private void OnLabelChanged(string newLabel)
        {
            SendMessage(new HandLabelerLabelChangedMessage(newLabel));
            Close();
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not HandLabelerBoundUserInterfaceState cast)
                return;

            _window.SetCurrentLabel(cast.CurrentLabel);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
