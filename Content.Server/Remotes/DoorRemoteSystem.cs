using Robust.Shared.Player;
using Robust.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Physics;
using Content.Shared.Access.Components;
using Content.Server.Doors.Systems;
using Content.Server.Doors.Components;
using Content.Shared.Interaction.Events;

namespace Content.Server.Remotes
{
    public sealed class DoorRemoteSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly SharedAirlockSystem _sharedAirlockSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DoorRemoteComponent, UseInHandEvent>(OnInHandActivation);
            SubscribeLocalEvent<DoorRemoteComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
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

        private void OnBeforeInteract(EntityUid uid, DoorRemoteComponent component, BeforeRangedInteractEvent args)
        {
            if (!args.CanReach ||
                args.Handled
                || args.Target == null
                || !TryComp<DoorComponent>(args.Target, out var doorComponent) // If it isn't a door we don't use it
                || !HasComp<AccessReaderComponent>(args.Target) // Remotes do not work on doors without access requirements
                || !TryComp<AirlockComponent>(args.Target, out var airlockComponent) // Remotes only work on airlocks
                // TODO: Why the fuck is this -1f
                || !_interactionSystem.InRangeUnobstructed(args.User, doorComponent.Owner, -1f, CollisionGroup.Opaque))
            {
                return;
            }

            args.Handled = true;

            if (component.Mode == DoorRemoteComponent.OperatingMode.OpenClose)
            {
                _doorSystem.TryToggleDoor(doorComponent.Owner, user: args.Used);
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
                        _doorSystem.Deny(airlockComponent.Owner, user: args.User);
                    }
                    else if (doorComponent.DenySound != null)
                    {
                        SoundSystem.Play(doorComponent.DenySound.GetSound(), Filter.Pvs(args.Target.Value, entityManager: EntityManager), args.Target.Value);
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
