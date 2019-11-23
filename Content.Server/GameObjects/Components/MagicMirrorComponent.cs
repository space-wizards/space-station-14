using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components;
using Content.Shared.Preferences.Appearance;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MagicMirrorComponent : SharedMagicMirrorComponent, IActivate
    {
        private BoundUserInterface _userInterface;

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(MagicMirrorUiKey.Key);
            _userInterface.OnReceiveMessage += OnUiReceiveMessage;
        }

        private static void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            var hair = obj.Session.AttachedEntity.GetComponent<HairComponent>();
            switch (obj.Message)
            {
                case HairSelectedMessage msg:
                    var map =
                        msg.IsFacialHair ? HairStyles.FacialHairStylesMap : HairStyles.HairStylesMap;
                    if (!map.ContainsKey(msg.HairName))
                        return;

                    if (msg.IsFacialHair)
                    {
                        hair.FacialHairStyleName = msg.HairName;
                    }
                    else
                    {
                        hair.HairStyleName = msg.HairName;
                    }

                    break;
                case HairColorSelectedMessage msg:
                    if (msg.IsFacialHair)
                    {
                        hair.FacialHairColor = msg.HairColor;
                    }
                    else
                    {
                        hair.HairColor = msg.HairColor;
                    }

                    break;
            }
        }

        public void Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            _userInterface.Open(actor.playerSession);
        }
    }
}
