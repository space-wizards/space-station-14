using Content.Server.Administration.Logs;
using Content.Server.Doors.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
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
        [Dependency] private readonly SharedBoltSystem _bolt = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DoorRemoteComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        }

        private void OnBeforeInteract(Entity<DoorRemoteComponent> entity, ref BeforeRangedInteractEvent args)
        {
            var isAirlock = TryComp<AirlockComponent>(args.Target, out var airlockComp);
            var hasBolts = TryComp<DoorBoltComponent>(args.Target, out var boltsComp);
            TryComp<DoorComponent>(entity, out var doorComp);

            if (args.Handled || args.Target == null)
                return;

            if (entity.Comp.Mode == OperatingMode.ToggleBolts && !hasBolts)
                return;

            if (entity.Comp.Mode != OperatingMode.ToggleBolts && !isAirlock)
                return;

            if (!_examine.InRangeUnOccluded(args.User, args.Target.Value, SharedInteractionSystem.MaxRaycastRange))
                return;

            args.Handled = true;

            if (!this.IsPowered(args.Target.Value, EntityManager))
            {
                Popup.PopupEntity(Loc.GetString("door-remote-no-power"), args.User, args.User);
                return;
            }

            var accessTarget = args.Used;
            // This covers the accesses the REMOTE has, and is not effected by the user's ID card.
            if (entity.Comp.IncludeUserAccess) // Allows some door remotes to inherit the user's access.
            {
                accessTarget = args.User;
                // This covers the accesses the USER has, which always includes the remote's access since holding a remote acts like holding an ID card.
            }

            if (TryComp<AccessReaderComponent>(args.Target, out var accessComponent)
                && !_doorSystem.HasAccess(args.Target.Value, accessTarget, accessComponent))
            {
                if (isAirlock)
                    _doorSystem.Deny(args.Target.Value, doorComp, accessTarget);
                Popup.PopupEntity(Loc.GetString("door-remote-denied"), args.User, args.User);
                return;
            }

            switch (entity.Comp.Mode)
            {
                case OperatingMode.OpenClose:
                    if (doorComp != null && _doorSystem.TryToggleDoor(args.Target.Value, doorComp, accessTarget))
                    {
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp.State}");
                    }

                    break;
                case OperatingMode.ToggleBolts:
                    if(boltsComp is { BoltWireCut: false })
                    {
                        _bolt.TrySetBoltsToggle((args.Target.Value, boltsComp), accessTarget);
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
                case OperatingMode.placeholderForUiUpdates:
                default:
                    throw new InvalidOperationException(
                        $"{nameof(DoorRemoteComponent)} had invalid mode {entity.Comp.Mode}");
            }
        }
    }
}
