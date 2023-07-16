using Content.Shared.Access.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Access.UI
{
    /// <summary>
    /// Initializes a <see cref="AgentIDCardWindow"/> and updates it when new server messages are received.
    /// </summary>
    public sealed class AgentIDCardBoundUserInterface : BoundUserInterface
    {
        private AgentIDCardWindow? _window;

        public AgentIDCardBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new AgentIDCardWindow();
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnNameEntered += OnNameChanged;
            _window.OnJobEntered += OnJobChanged;
        }

        private void OnNameChanged(string newName)
        {
            SendMessage(new AgentIDCardNameChangedMessage(newName));
        }

        private void OnJobChanged(string newJob)
        {
            SendMessage(new AgentIDCardJobChangedMessage(newJob));
        }

        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not AgentIDCardBoundUserInterfaceState cast)
                return;

            _window.SetCurrentName(cast.CurrentName);
            _window.SetCurrentJob(cast.CurrentJob);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
