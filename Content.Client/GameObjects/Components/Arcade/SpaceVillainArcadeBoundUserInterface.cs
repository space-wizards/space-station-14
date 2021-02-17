using Content.Client.Arcade;
using Content.Shared.GameObjects.Components.Arcade;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Arcade
{
    public class SpaceVillainArcadeBoundUserInterface : BoundUserInterface
    {
        [ViewVariables] private SpaceVillainArcadeMenu _menu;

        //public SharedSpaceVillainArcadeComponent SpaceVillainArcade;

        public SpaceVillainArcadeBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
            SendAction(SharedSpaceVillainArcadeComponent.PlayerAction.RequestData);
        }

        public void SendAction(SharedSpaceVillainArcadeComponent.PlayerAction action)
        {
            SendMessage(new SharedSpaceVillainArcadeComponent.SpaceVillainArcadePlayerActionMessage(action));
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
            if(message is SharedSpaceVillainArcadeComponent.SpaceVillainArcadeDataUpdateMessage msg) _menu.UpdateInfo(msg);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _menu?.Dispose();
        }
    }
}
