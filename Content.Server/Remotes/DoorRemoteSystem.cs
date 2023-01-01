using Robust.Shared.Player;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Physics;
using Content.Shared.Access.Components;
using Content.Server.Doors.Systems;
using Content.Server.Doors.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Interaction.Events;
using static Content.Server.Remotes.DoorRemoteComponent;

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
            SubscribeLocalEvent<DoorRemoteComponent, UseInHandEvent>(OnInHandActivation);
            SubscribeLocalEvent<DoorRemoteComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        }

        public void OnInHandActivation(EntityUid user, DoorRemoteComponent component, UseInHandEvent args)
        {
            string switchMessageId;
            switch (component.Mode)
            {
                case OperatingMode.OpenClose:
                    component.Mode = OperatingMode.ToggleBolts;
                    switchMessageId = "door-remote-switch-state-toggle-bolts";
                    break;
                case OperatingMode.ToggleBolts:
                    component.Mode = OperatingMode.ToggleEmergencyAccess;
                    switchMessageId = "door-remote-switch-state-toggle-emergency-access";
                    break;
                case OperatingMode.ToggleEmergencyAccess:
                    component.Mode = OperatingMode.OpenClose;
                    switchMessageId = "door-remote-switch-state-open-close";
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{nameof(DoorRemoteComponent)} had invalid mode {component.Mode}");
            }
            ShowPopupToUser(switchMessageId, args.User);
        }

        private void OnBeforeInteract(EntityUid uid, DoorRemoteComponent component, BeforeRangedInteractEvent args)
        {
            if (args.Handled
                || args.Target == null
                || !TryComp<DoorComponent>(args.Target, out var doorComp) // If it isn't a door we don't use it
                || !TryComp<AirlockComponent>(args.Target, out var airlockComp) // Remotes only work on airlocks
                // The remote can be used anywhere the user can see the door.
                // This doesn't work that well, but I don't know of an alternative
                || !_interactionSystem.InRangeUnobstructed(args.User, args.Target.Value,
                    SharedInteractionSystem.MaxRaycastRange, CollisionGroup.Opaque))
                return;

            args.Handled = true;

            if (!this.IsPowered(args.Target.Value, EntityManager))
            {
                ShowPopupToUser("door-remote-no-power", args.User);
                return;
            }

            if (TryComp<AccessReaderComponent>(args.Target, out var accessComponent) &&
                !_doorSystem.HasAccess(doorComp.Owner, args.Used, accessComponent))
            {
                _doorSystem.Deny(airlockComp.Owner, doorComp, args.User);
                ShowPopupToUser("door-remote-denied", args.User);
                return;
            }

            switch (component.Mode)
            {
                case OperatingMode.OpenClose:
                    _doorSystem.TryToggleDoor(doorComp.Owner, doorComp, args.Used);
                    break;
                case OperatingMode.ToggleBolts:
                    if (!airlockComp.BoltWireCut)
                        airlockComp.SetBoltsWithAudio(!airlockComp.IsBolted());
                    break;
                case OperatingMode.ToggleEmergencyAccess:
                    _sharedAirlockSystem.ToggleEmergencyAccess(airlockComp);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{nameof(DoorRemoteComponent)} had invalid mode {component.Mode}");
            }
        }

        private void ShowPopupToUser(string messageId, EntityUid user) =>
            _popupSystem.PopupEntity(Loc.GetString(messageId), user, user);
    }
}
