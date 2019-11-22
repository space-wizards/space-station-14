using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components;
using Content.Shared.Preferences.Appearance;
using Robust.Server.GameObjects;
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
            AppearanceComponent appearance = obj.Session.AttachedEntity.GetComponent<AppearanceComponent>();
            switch (obj.Message)
            {
                case HairSelectedMessage msg:
                    var map =
                        msg.IsFacialHair ? HairStyles.FacialHairStylesMap : HairStyles.HairStylesMap;
                    var visual =
                        msg.IsFacialHair ? CharacterVisuals.FacialHairStyle : CharacterVisuals.HairStyle;
                    if (!map.ContainsKey(msg.HairName))
                        return;
                    appearance.SetData(visual, msg.HairName);
                    break;
                case HairColorSelectedMessage msg:
                    appearance.SetData(msg.IsFacialHair ? CharacterVisuals.FacialHairColor : CharacterVisuals.HairColor,
                        msg.HairColor);
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
