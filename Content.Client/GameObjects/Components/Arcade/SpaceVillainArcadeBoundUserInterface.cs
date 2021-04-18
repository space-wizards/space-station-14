using Content.Client.Arcade;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Arcade.SharedSpaceVillainArcadeComponent;

namespace Content.Client.GameObjects.Components.Arcade
{
    public class SpaceVillainArcadeBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private SpaceVillainArcadeMenu? _menu;

        //public SharedSpaceVillainArcadeComponent SpaceVillainArcade;

        public SpaceVillainArcadeBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
            SendAction(PlayerAction.RequestData);
        }

        public void SendAction(PlayerAction action)
        {
            SendMessage(new SpaceVillainArcadePlayerActionMessage(action));
        }

        protected override void Open()
        {
            base.Open();

            /*if(!Owner.Owner.TryGetComponent(out SharedSpaceVillainArcadeComponent spaceVillainArcade))
            {
                return;
            }

            SpaceVillainArcade = spaceVillainArcade;*/

            _menu = new SpaceVillainArcadeMenu(this);

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (message is SpaceVillainArcadeDataUpdateMessage msg) _menu?.UpdateInfo(msg);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing) _menu?.Dispose();
        }
    }
}
