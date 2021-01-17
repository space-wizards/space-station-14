#nullable enable
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Disposal;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    public class DisposalTagGunComponent : SharedDisposalTagGunComponent, IAfterInteract, IUse
    {
        public override string Name => "DisposalTagGun";
        [ViewVariables(VVAccess.ReadWrite)]
        private string _tag = "";
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalTagGunUIKey.Key);

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnReceiveMessage;
            }
        }

        private void OnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case TagChangedMessage msg:
                    _tag = msg.Tag;
                    UserInterface?.SendMessage(new TagChangedMessage(_tag));
                    return;
            }
        }

        public async Task AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if(eventArgs.Target == null) return;
            var tagComp = eventArgs.Target.EnsureComponent<DisposalTagComponent>();
            tagComp.Tag = _tag;
            eventArgs.User.PopupMessage(Loc.GetString("You label {0:theName} as \"{1}\".", eventArgs.Target, _tag));
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            var session = eventArgs.User.PlayerSession();
            if (session != null)
            {
                UserInterface?.Open(session);
                UserInterface?.SendMessage(new TagChangedMessage(_tag), session);
            }

            return true;
        }
    }
}
