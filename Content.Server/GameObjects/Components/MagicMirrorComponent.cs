using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Preferences.Appearance;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

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
            if (!obj.Session.AttachedEntity.TryGetComponent(out HairComponent hair))
            {
                return;
            }

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
                    var (r, g, b) = msg.HairColor;
                    var color = new Color(r, g, b);

                    if (msg.IsFacialHair)
                    {
                        hair.FacialHairColor = color;
                    }
                    else
                    {
                        hair.HairColor = color;
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

            if (!eventArgs.User.TryGetComponent(out HairComponent hair))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't have any hair!"));
                return;
            }

            _userInterface.Open(actor.playerSession);

            var msg = new MagicMirrorInitialDataMessage(hair.HairColor, hair.FacialHairColor, hair.HairStyleName,
                hair.FacialHairStyleName);

            _userInterface.SendMessage(msg, actor.playerSession);
        }
    }
}
