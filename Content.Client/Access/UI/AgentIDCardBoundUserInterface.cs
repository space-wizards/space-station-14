using Content.Shared.Access.Components;
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
    public sealed class AgentIDCardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
    {
        private AgentIDCardWindow? _window;

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<AgentIDCardWindow>();

            _window.OnNameChanged += OnNameChanged;
            _window.OnJobChanged += OnJobChanged;
            _window.OnJobIconChanged += OnJobIconChanged;
            Update();
        }

        public override void Update()
        {
            base.Update();

            if (_window == null)
                return;

            if (!EntMan.TryGetComponent(Owner, out IdCardComponent? card) ||
                !EntMan.TryGetComponent(Owner, out AgentIDCardComponent? agent))
                return;

            _window.SetCurrentName(card.FullName ?? string.Empty);
            _window.SetCurrentJob(card.LocalizedJobTitle ?? string.Empty);
            _window.SetAllowedIcons(card.JobIcon, agent.IconGroups);
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
    }
}
