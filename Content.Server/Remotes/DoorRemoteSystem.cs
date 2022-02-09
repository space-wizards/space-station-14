using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.IoC;
using Robust.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Physics;
using Content.Shared.Access.Components;
using Content.Server.Doors.Systems;
using Content.Server.Doors.Components;

namespace Content.Server.Remotes
{
    public sealed class DoorRemoteSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedDoorSystem _sharedDoorSystem = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly SharedAirlockSystem _sharedAirlockSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DoorRemoteComponent, UseInHandEvent>(OnInHandActivation);
            SubscribeLocalEvent<DoorRemoteComponent, AfterInteractEvent>(OnAfterInteract);
        }

        public void OnInHandActivation(EntityUid user, DoorRemoteComponent component, UseInHandEvent args)
        {
            switch (component.Mode)
            {
                case DoorRemoteComponent.OperatingMode.OpenClose:
                    component.Mode = DoorRemoteComponent.OperatingMode.ToggleBolts;
                    _popupSystem.PopupEntity(Loc.GetString("door-remote-switch-state-toggle-bolts"), args.User, Filter.Entities(args.User));
                    break;
                case DoorRemoteComponent.OperatingMode.ToggleBolts:
                    component.Mode = DoorRemoteComponent.OperatingMode.ToggleEmergencyAccess;
                    _popupSystem.PopupEntity(Loc.GetString("door-remote-switch-state-toggle-emergency-access"), args.User, Filter.Entities(args.User));
                    break;
                case DoorRemoteComponent.OperatingMode.ToggleEmergencyAccess:
                    component.Mode = DoorRemoteComponent.OperatingMode.OpenClose;
                    _popupSystem.PopupEntity(Loc.GetString("door-remote-switch-state-open-close"), args.User, Filter.Entities(args.User));
                    break;
            }
        }

        private void OnAfterInteract(EntityUid uid, DoorRemoteComponent component, AfterInteractEvent args)
        {
            if (args.Handled
                || args.Target == null
                || !TryComp<DoorComponent>(args.Target, out var doorComponent) // If it isn't a door we don't use it
                || !HasComp<AccessReaderComponent>(args.Target) // Remotes do not work on doors without access requirements
                || !TryComp<AirlockComponent>(args.Target, out var airlockComponent) // Remotes only work on airlocks
                || !_interactionSystem.InRangeUnobstructed(args.User, doorComponent.Owner, -1f, CollisionGroup.Opaque))
            {
                return;
            }

            args.Handled = true;
            
            if (component.Mode == DoorRemoteComponent.OperatingMode.OpenClose)
            {
                _sharedDoorSystem.TryToggleDoor(doorComponent.Owner, user: args.Used);
            }

            if (component.Mode == DoorRemoteComponent.OperatingMode.ToggleBolts
                && airlockComponent.IsPowered())
            {
                if (_doorSystem.HasAccess(doorComponent.Owner, args.Used))
                {
                    airlockComponent.SetBoltsWithAudio(!airlockComponent.IsBolted());
                }
                else
                {
                    if (doorComponent.State != DoorState.Open)
                    {
                        _sharedDoorSystem.Deny(airlockComponent.Owner, user: args.User);
                    }
                    else if (doorComponent.DenySound != null)
                    {
                        SoundSystem.Play(Filter.Pvs(args.Target.Value), doorComponent.DenySound.GetSound(), args.Target.Value);
                    }
                }
            }

            if (component.Mode == DoorRemoteComponent.OperatingMode.ToggleEmergencyAccess
                && airlockComponent.IsPowered())
            {
                if (_doorSystem.HasAccess(doorComponent.Owner, args.Used))
                {
                    _sharedAirlockSystem.ToggleEmergencyAccess(airlockComponent);
                }
            }
        }
    }
}
