#nullable enable
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Preferences.Appearance;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MagicMirrorComponent : SharedMagicMirrorComponent, IActivate
    {
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(MagicMirrorUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }
        }

        public override void OnRemove()
        {
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= OnUiReceiveMessage;
            }

            base.OnRemove();
        }

        private static void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            if (!obj.Session.AttachedEntity.TryGetComponent(out HumanoidAppearanceComponent? looks))
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

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (!eventArgs.User.TryGetComponent(out HumanoidAppearanceComponent? looks))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You can't have any hair!"));
                return;
            }

            UserInterface?.Toggle(actor.playerSession);

            var msg = new MagicMirrorInitialDataMessage(looks.Appearance.HairColor, looks.Appearance.FacialHairColor, looks.Appearance.HairStyleName,
                looks.Appearance.FacialHairStyleName);

            UserInterface?.SendMessage(msg, actor.playerSession);
        }
    }
}
