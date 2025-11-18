using Content.Shared.Access.Systems;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Access.UI
{
    /// <summary>
    /// Initializes a <see cref="AgentIDCardWindow"/> and updates it when new server messages are received.
    /// </summary>
    public sealed class AgentIDCardBoundUserInterface : BoundUserInterface
    {
        private AgentIDCardWindow? _window;

        public AgentIDCardBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<AgentIDCardWindow>();

            _window.OnNameChanged += OnNameChanged;
            _window.OnJobChanged += OnJobChanged;
            _window.OnJobIconChanged += OnJobIconChanged;
        }

        private void OnNameChanged(string newName)
        {
            SendMessage(new AgentIDCardNameChangedMessage(newName));
        }

        private void OnJobChanged(string newJob)
        {
            SendMessage(new AgentIDCardJobChangedMessage(newJob));
        }

        public void OnJobIconChanged(ProtoId<JobIconPrototype> newJobIconId)
        {
            SendMessage(new AgentIDCardJobIconChangedMessage(newJobIconId));
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
            _window.SetAllowedIcons(cast.CurrentJobIconId);
        }
    }
}
