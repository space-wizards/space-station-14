using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Remotes.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Remotes.EntitySystems;

public abstract class SharedDoorRemoteSystem : EntitySystem
{
    [Dependency] private readonly SharedAirlockSystem _airlock = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrify = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<DoorRemoteComponent, DoorRemoteModeChangeMessage>(OnDoorRemoteModeChange);
        SubscribeLocalEvent<DoorRemoteComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
    }

    private void OnDoorRemoteModeChange(Entity<DoorRemoteComponent> ent, ref DoorRemoteModeChangeMessage args)
    {
        ent.Comp.Mode = args.Mode;
        Dirty(ent);
    }

    private void OnBeforeInteract(Entity<DoorRemoteComponent> entity, ref BeforeRangedInteractEvent args)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var isAirlock = TryComp<AirlockComponent>(args.Target, out var airlockComp);

        if (args.Handled
            || args.Target == null
            || !TryComp<DoorComponent>(args.Target, out var doorComp) // If it isn't a door we don't use it
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

        if (!_powerReceiver.IsPowered(args.Target.Value))
        {
            _popup.PopupClient(Loc.GetString("door-remote-no-power"), args.User, args.User);
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
            && !_doorSystem.HasAccess(args.Target.Value, accessTarget, doorComp, accessComponent))
        {
            if (isAirlock)
                _doorSystem.Deny(args.Target.Value, doorComp, user: args.User, predicted: true);

            _popup.PopupClient(Loc.GetString("door-remote-denied"), args.User, args.User);
            return;
        }

        switch (entity.Comp.Mode)
        {
            case OperatingMode.OpenClose:
                if (_doorSystem.TryToggleDoor(args.Target.Value, doorComp, user: args.User, predicted: true))
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)}: {doorComp.State}");
                break;
            case OperatingMode.ToggleBolts:
                if (TryComp<DoorBoltComponent>(args.Target, out var boltsComp))
                {
                    if (!boltsComp.BoltWireCut)
                    {
                        _doorSystem.SetBoltsDown((args.Target.Value, boltsComp), !boltsComp.BoltsDown, user: args.User, predicted: true);
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to {(boltsComp.BoltsDown ? "" : "un")}bolt it");
                    }
                }

                break;
            case OperatingMode.ToggleEmergencyAccess:
                if (airlockComp != null)
                {
                    _airlock.SetEmergencyAccess((args.Target.Value, airlockComp), !airlockComp.EmergencyAccess, user: args.User, predicted: true);
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to set emergency access {(airlockComp.EmergencyAccess ? "on" : "off")}");
                }

                break;
            case OperatingMode.ToggleOvercharge:
                if (TryComp<ElectrifiedComponent>(args.Target, out var eletrifiedComp))
                {
                    _electrify.SetElectrified((args.Target.Value, eletrifiedComp), !eletrifiedComp.Enabled);
                    var soundToPlay = eletrifiedComp.Enabled
                        ? eletrifiedComp.AirlockElectrifyDisabled
                        : eletrifiedComp.AirlockElectrifyEnabled;
                    _audio.PlayLocal(soundToPlay, args.Target.Value, args.User);
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(args.User):player} used {ToPrettyString(args.Used)} on {ToPrettyString(args.Target.Value)} to {(eletrifiedComp.Enabled ? "" : "un")}electrify it");
                }

                break;
            default:
                throw new InvalidOperationException(
                    $"{nameof(DoorRemoteComponent)} had invalid mode {entity.Comp.Mode}");
        }
    }
}

[Serializable, NetSerializable]
public sealed class DoorRemoteModeChangeMessage : BoundUserInterfaceMessage
{
    public OperatingMode Mode;
}

[Serializable, NetSerializable]
public enum DoorRemoteUiKey : byte
{
    Key
}
