using Content.Server.Administration.Logs;
using Content.Server.Doors.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Remotes.Components;
using Content.Shared.Remotes.EntitySystems;

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
            if (args.Handled || args.Target == null)
                return;
            var isAirlock = TryComp<AirlockComponent>(args.Target, out var airlockComp);
            var isDoor = TryComp<DoorComponent>(args.Target, out var doorComp);
            var hasBolts = TryComp<DoorBoltComponent>(args.Target, out var boltsComp);
            if( !isDoor && !hasBolts // If it isn't a door and it doesn't have bolts we don't use it
                // Only able to control doors if they are within your vision and within your max range.
                // Not affected by mobs or machines anymore.
                || !_examine.InRangeUnOccluded(args.User,
                    args.Target.Value,
                    SharedInteractionSystem.MaxRaycastRange,
                    null))

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
                if (isAirlock)
                    _doorSystem.Deny(args.Target.Value, doorComp, args.User);
                Popup.PopupEntity(Loc.GetString("door-remote-denied"), args.User, args.User);
                return;
            }

            switch (entity.Comp.Mode)
            {
                case OperatingMode.OpenClose:
                    if (doorComp != null && _doorSystem.TryToggleDoor(args.Target.Value, doorComp, args.Used))
                    {
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp.State}");
                    }

                    break;
                case OperatingMode.ToggleBolts:
                    if (boltsComp is { BoltWireCut: false })
                    {
                        _doorSystem.SetBoltsDown((args.Target.Value, boltsComp), !boltsComp.BoltsDown, args.Used);
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to {(boltsComp.BoltsDown ? "" : "un")}bolt it");
                    }

                    break;
                case OperatingMode.ToggleEmergencyAccess:
                    if (airlockComp != null)
                    {
                        _airlock.SetEmergencyAccess((args.Target.Value, airlockComp), !airlockComp.EmergencyAccess);
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
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
