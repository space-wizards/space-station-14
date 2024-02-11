using Content.Server.Administration.Logs;
using Robust.Shared.Player;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Physics;
using Content.Shared.Access.Components;
using Content.Server.Doors.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using static Content.Server.Remotes.DoorRemoteComponent;

namespace Content.Server.Remotes
{
    public sealed class DoorRemoteSystem : EntitySystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly DoorBoltSystem _bolts = default!;
        [Dependency] private readonly AirlockSystem _airlock = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        // I'm so sorry [Dependency] private readonly SharedAirlockSystem _sharedAirlockSystem = default!;

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

                    // Skip toggle bolts mode and move on from there (to emergency access)
                case OperatingMode.ToggleBolts:
                    component.Mode = OperatingMode.ToggleEmergencyAccess;
                    switchMessageId = "door-remote-switch-state-toggle-emergency-access";
                    break;

                    // Skip ToggleEmergencyAccess mode and move on from there (to door toggle)
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
            bool isAirlock = TryComp<AirlockComponent>(args.Target, out var airlockComp);

            if (args.Handled
                || args.Target == null
                || !TryComp<DoorComponent>(args.Target, out var doorComp) // If it isn't a door we don't use it
                // Only able to control doors if they are within your vision and within your max range.
                // Not affected by mobs or machines anymore.
                || !ExamineSystemShared.InRangeUnOccluded(args.User, args.Target.Value, SharedInteractionSystem.MaxRaycastRange, null))
            {
                return;
            }

            args.Handled = true;

            if (!this.IsPowered(args.Target.Value, EntityManager))
            {
                ShowPopupToUser("door-remote-no-power", args.User);
                return;
            }

            // Holding the door remote grants you access to the relevant doors IN ADDITION to what ever access you had.
            // This access is enforced in _doorSystem.HasAccess when it calls _accessReaderSystem.IsAllowed
            if (TryComp<AccessReaderComponent>(args.Target, out var accessComponent)
                && !_doorSystem.HasAccess(args.Target.Value, args.User, doorComp, accessComponent))
            {
                _doorSystem.Deny(args.Target.Value, doorComp, args.User);
                ShowPopupToUser("door-remote-denied", args.User);
                return;
            }

            switch (component.Mode)
            {
                case OperatingMode.OpenClose:
                    // Note we provide args.User here to TryToggleDoor as the "user"
                    // This means that the door will look at all access items carryed by the player for access, including
                    // this remote, but also including anything else they are carrying such as a PDA or ID card.
                    if (_doorSystem.TryToggleDoor(args.Target.Value, doorComp, args.User))
                        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp.State}");
                    break;
                case OperatingMode.ToggleBolts:
                    if (TryComp<DoorBoltComponent>(args.Target, out var boltsComp))
                    {
                        if (!boltsComp.BoltWireCut)
                        {
                            _bolts.SetBoltsWithAudio(args.Target.Value, boltsComp, !boltsComp.BoltsDown);
                            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to {(boltsComp.BoltsDown ? "" : "un")}bolt it");
                        }
                    }
                    break;
                case OperatingMode.ToggleEmergencyAccess:
                    if (airlockComp != null)
                    {
                        _airlock.ToggleEmergencyAccess(args.Target.Value, airlockComp);
                        _adminLogger.Add(LogType.Action, LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to set emergency access {(airlockComp.EmergencyAccess ? "on" : "off")}");
                    }
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
