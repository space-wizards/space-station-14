using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server.Remotes
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
            var popupSystem = EntityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();

            switch (component.Mode)
            {
                case DoorRemoteComponent.OperatingMode.OpenClose:
                    component.Mode = DoorRemoteComponent.OperatingMode.ToggleBolts;
                    popupSystem.PopupEntity(Loc.GetString("door-remote-switch-state-toggle-bolts"), args.User, Filter.Entities(args.User));
                    break;
                case DoorRemoteComponent.OperatingMode.ToggleBolts:
                    component.Mode = DoorRemoteComponent.OperatingMode.OpenClose; // TODO: Swítch to ToggleEmergencyAcces when EA is implemented
                    popupSystem.PopupEntity(Loc.GetString("door-remote-switch-state-open-close"), args.User, Filter.Entities(args.User)); // TODO: See the above comment
                    break;
            /*
                case DoorRemoteComponent.OperatingMode.ToggleEmergencyAccess:
                    component.Mode = DoorRemoteComponent.OperatingMode.OpenClose;
                    popupSystem.PopupEntity(Loc.GetString("door-remote-switch-state-open-close"), args.User, Filter.Entities(args.User));
                    break;
            */
            }
        }
    }
}
