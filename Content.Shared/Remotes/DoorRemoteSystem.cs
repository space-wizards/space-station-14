using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Content.Shared.Popups;
using Content.Shared.Interaction;

namespace Content.Shared.Remotes
{
    public sealed class DoorRemoteSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DoorRemoteComponent, UseInHandEvent>(OnInHandActivation);
        }

        public void OnInHandActivation(EntityUid user, DoorRemoteComponent component, UseInHandEvent args)
        {
            switch (component.Mode)
            {
                case DoorRemoteComponent.OperatingMode.OpenClose:
                    component.Mode = DoorRemoteComponent.OperatingMode.ToggleBolts;
                    args.User.PopupMessage(Loc.GetString("door-remote-switch-state-toggle-bolts"));
                    break;
                case DoorRemoteComponent.OperatingMode.ToggleBolts:
                    component.Mode = DoorRemoteComponent.OperatingMode.OpenClose; // TODO: Swítch to ToggleEmergencyAcces when EA is implemented
                    args.User.PopupMessage(Loc.GetString("door-remote-switch-state-open-close")); // TODO: See the above comment
                    break;
            /*
                case DoorRemoteComponent.OperatingMode.ToggleEmergencyAccess:
                    component.Mode = DoorRemoteComponent.OperatingMode.OpenClose;
                    args.User.PopupMessage(Loc.GetString("door-remote-switch-state-open-close"));
                    break;
            */
            }
        }
    }
}
