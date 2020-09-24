using Content.Client.Arcade;
using Content.Shared.Arcade;
using Content.Shared.GameObjects.Components.Arcade;
using JetBrains.Annotations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Client.GameObjects.Components.Arcade
{
    public class TetrisArcadeBoundUserInterface : BoundUserInterface
    {
        private TetrisArcadeMenu _menu;

        public TetrisArcadeBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new TetrisArcadeMenu(this);
            _menu.OnClose += Close;
            _menu.OpenCentered();
            SendAction(TetrisPlayerAction.NewGame);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (!(message is TetrisMessages.TetrisUIUpdateMessage msg)) return;

            _menu?.UpdateBlocks(msg.Blocks);
        }

        public void StartGame()
        {
            SendAction(TetrisPlayerAction.StartGame);
        }

        public void SendAction(TetrisPlayerAction action)
        {
            SendMessage(new TetrisMessages.TetrisPlayerActionMessage(action));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(!disposing) { return; }
            _menu?.Dispose();
        }
    }
}
