using Content.Shared.GameObjects.Components.Research;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Client.GameObjects.Components.Research
{
    public class ResearchClientBoundUserInterface : BoundUserInterface
    {
        private ResearchClientServerSelectionMenu _menu;

        public int ServerCount = 0;
        public string[] ServerNames = new string[]{};
        public int[] ServerIds = new int[]{};
        public int SelectedServerId = -1;

        public ResearchClientBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendMessage(new SharedResearchClientComponent.ResearchClientSyncMessage());
        }

        protected override void Open()
        {
            base.Open();

            _menu = new ResearchClientServerSelectionMenu() { Owner = this };

            _menu.OnClose += Close;

            _menu.OpenCentered();
        }

        public void SelectServer(int serverId)
        {
            SendMessage(new SharedResearchClientComponent.ResearchClientServerSelectedMessage(serverId));
        }

        public void DeselectServer()
        {
            SendMessage(new SharedResearchClientComponent.ResearchClientServerDeselectedMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (!(state is SharedResearchClientComponent.ResearchClientBoundInterfaceState rstate)) return;
            ServerCount = rstate.ServerCount;
            ServerNames = rstate.ServerNames;
            ServerIds = rstate.ServerIds;
            SelectedServerId = rstate.SelectedServerId;
            _menu.Populate();

        }
    }
}
