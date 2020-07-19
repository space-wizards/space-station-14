using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
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
            if (!obj.Session.AttachedEntity.TryGetComponent(out HumanoidAppearanceComponent looks))
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
                        looks.Appearance = looks.Appearance.WithFacialHairStyleName(msg.HairName);
                    }
                    else
                    {
                        looks.Appearance = looks.Appearance.WithHairStyleName(msg.HairName);
                    }

                    break;

                case HairColorSelectedMessage msg:
                    var (r, g, b) = msg.HairColor;
                    var color = new Color(r, g, b);

                    if (msg.IsFacialHair)
                    {
                        looks.Appearance = looks.Appearance.WithFacialHairColor(color);
                    }
                    else
                    {
                        looks.Appearance = looks.Appearance.WithHairColor(color);
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

            if (!eventArgs.User.TryGetComponent(out HumanoidAppearanceComponent looks))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't have any hair!"));
                return;
            }

            _userInterface.Open(actor.playerSession);

            var msg = new MagicMirrorInitialDataMessage(looks.Appearance.HairColor, looks.Appearance.FacialHairColor, looks.Appearance.HairStyleName,
                looks.Appearance.FacialHairStyleName);

            _userInterface.SendMessage(msg, actor.playerSession);
        }
    }
}
