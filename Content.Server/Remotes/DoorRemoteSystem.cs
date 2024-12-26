using Content.Server.Administration.Logs;
using Content.Shared.Interaction;
using Content.Shared.Doors.Components;
using Content.Shared.Access.Components;
using Content.Server.Doors.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Remotes.EntitySystems;
using Content.Shared.Remotes.Components;

namespace Content.Shared.Remotes
{
    public sealed class DoorRemoteSystem : SharedDoorRemoteSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AirlockSystem _airlock = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DoorRemoteComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        }

        private void OnBeforeInteract(Entity<DoorRemoteComponent> entity, ref BeforeRangedInteractEvent args)
        {
            bool isAirlock = TryComp<AirlockComponent>(args.Target, out var airlockComp);

            if (args.Handled
                || args.Target == null
                || !TryComp<DoorComponent>(args.Target, out var doorComp) // If it isn't a door we don't use it
                                                                          // Only able to control doors if they are within your vision and within your max range.
                                                                          // Not affected by mobs or machines anymore.
                || !_examine.InRangeUnOccluded(args.User, args.Target.Value, SharedInteractionSystem.MaxRaycastRange, null))

            {
                return;
            }

            args.Handled = true;

            if (!this.IsPowered(args.Target.Value, EntityManager))
            {
                Popup.PopupEntity(Loc.GetString("door-remote-no-power"), args.User, args.User);
                return;
            }

            if (TryComp<AccessReaderComponent>(args.Target, out var accessComponent)
                && !_doorSystem.HasAccess(args.Target.Value, args.Used, doorComp, accessComponent))
            {
                _doorSystem.Deny(args.Target.Value, doorComp, args.User);
                Popup.PopupEntity(Loc.GetString("door-remote-denied"), args.User, args.User);
                return;
            }

            switch (entity.Comp.Mode)
            {
                case OperatingMode.OpenClose:
                    if (_doorSystem.TryToggleDoor(args.Target.Value, doorComp, args.Used))
                        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp.State}");
                    break;
                case OperatingMode.ToggleBolts:
                    if (TryComp<DoorBoltComponent>(args.Target, out var boltsComp))
                    {
                        if (!boltsComp.BoltWireCut)
                        {
                            _doorSystem.SetBoltsDown((args.Target.Value, boltsComp), !boltsComp.BoltsDown, args.Used);
                            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to {(boltsComp.BoltsDown ? "" : "un")}bolt it");
                        }
                    }
                    break;
                case OperatingMode.ToggleEmergencyAccess:
                    if (airlockComp != null)
                    {
                        _airlock.SetEmergencyAccess((args.Target.Value, airlockComp), !airlockComp.EmergencyAccess);
                        _adminLogger.Add(LogType.Action, LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to set emergency access {(airlockComp.EmergencyAccess ? "on" : "off")}");
                    }
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{nameof(DoorRemoteComponent)} had invalid mode {entity.Comp.Mode}");
            }
        }
    }
}
