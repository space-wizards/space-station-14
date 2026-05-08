using Content.Shared.Research.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Research.UI
{
    public sealed class ResearchClientBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ResearchClientServerSelectionMenu? _menu;

        public ResearchClientBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            SendMessage(new ResearchClientSyncMessage());
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<ResearchClientServerSelectionMenu>();
            _menu.OnServerSelected += SelectServer;
            _menu.OnServerDeselected += DeselectServer;
        }

        public void SelectServer(int serverId)
        {
            SendMessage(new ResearchClientServerSelectedMessage(serverId));
        }

        public void DeselectServer()
        {
            SendMessage(new ResearchClientServerDeselectedMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not ResearchClientBoundInterfaceState rState) return;
            _menu?.Populate(rState.ServerCount, rState.ServerNames, rState.ServerIds, rState.SelectedServerId);
        }
    }
}
