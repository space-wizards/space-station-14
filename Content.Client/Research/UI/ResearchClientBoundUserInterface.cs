using Content.Shared.Research.Components;
using Robust.Client.GameObjects;

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

            _menu = new ResearchClientServerSelectionMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _menu?.Dispose();
        }
    }
}
