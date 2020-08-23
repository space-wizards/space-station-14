#nullable enable
using System.Threading.Tasks;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Paper
{
    [RegisterComponent]
    public class PaperComponent : SharedPaperComponent, IExamine, IInteractUsing, IUse
    {
        private string _content = "";
        private PaperAction _mode;

        private BoundUserInterface? UserInterface =>
            Owner.TryGetComponent(out ServerUserInterfaceComponent? ui) &&
            ui.TryGetBoundUserInterface(PaperUiKey.Key, out var boundUi)
                ? boundUi
                : null;

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            _mode = PaperAction.Read;
            UpdateUserInterface();
        }
        private void UpdateUserInterface()
        {
            UserInterface?.SetState(new PaperBoundUserInterfaceState(_content, _mode));
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange)
                return;

            message.AddMarkup(_content);
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
                return false;

            _mode = PaperAction.Read;
            UpdateUserInterface();
            UserInterface?.Open(actor.playerSession);
            return true;
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            var msg = (PaperInputText) obj.Message;
            if (string.IsNullOrEmpty(msg.Text))
                return;

            _content += msg.Text + '\n';

            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.LayerSetState(1, "paper_words");
            }

            UpdateUserInterface();
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.HasComponent<WriteComponent>())
                return false;
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
                return false;

            _mode = PaperAction.Write;
            UpdateUserInterface();
            UserInterface?.Open(actor.playerSession);
            return true;
        }
    }
}
