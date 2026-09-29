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
using Content.Shared.Tag;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Remotes.EntitySystems;

public abstract partial class SharedDoorRemoteSystem : EntitySystem
{
    [Dependency] private SharedAirlockSystem _airlock = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoorSystem _doorSystem = default!;
    [Dependency] private SharedElectrocutionSystem _electrify = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] protected IGameTiming Timing = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<DoorRemoteComponent, DoorRemoteModeChangeMessage>(OnDoorRemoteModeChange);
        SubscribeLocalEvent<DoorRemoteComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        SubscribeLocalEvent<DoorRemoteComponent, StationAiDoorRemoteInteractEvent>(OnStationAiInteract);
    }

    private void OnDoorRemoteModeChange(Entity<DoorRemoteComponent> ent, ref DoorRemoteModeChangeMessage args)
    {
        ent.Comp.Mode = args.Mode;
        Dirty(ent);
    }

    private void OnBeforeInteract(Entity<DoorRemoteComponent> entity, ref BeforeRangedInteractEvent args)
    {
        HandleInteraction(entity, args.User, args.Used, args.Target, ref args);
    }

    private void OnStationAiInteract(Entity<DoorRemoteComponent> entity, ref StationAiDoorRemoteInteractEvent args)
    {
        var interaction = new BeforeRangedInteractEvent(args.User, entity.Owner, args.Target, Transform(args.Target).Coordinates, true);
        HandleInteraction(entity, args.User, entity.Owner, args.Target, ref interaction, args.RangeUser);
        args.Handled = interaction.Handled;
    }

    private void HandleInteraction(
        Entity<DoorRemoteComponent> entity,
        EntityUid user,
        EntityUid used,
        EntityUid? target,
        ref BeforeRangedInteractEvent args,
        EntityUid? rangeUser = null)
    {
        var isAirlock = TryComp<AirlockComponent>(target, out var airlockComp);

        if (args.Handled
            || target == null
            || !TryComp<DoorComponent>(target, out var doorComp) // If it isn't a door we don't use it
                                                                      // Only able to control doors if they are within your vision and within your max range.
                                                                      // Not affected by mobs or machines anymore.
            || (entity.Comp.RequireInRangeUnoccluded && !_examine.InRangeUnOccluded(rangeUser ?? user,
                target.Value,
                SharedInteractionSystem.MaxRaycastRange,
                null)))

        {
            return;
        }

        args.Handled = true;

        if (!Timing.IsFirstTimePredicted)
            return;

        if (!_powerReceiver.IsPowered(target.Value))
        {
            _popup.PopupEntity(Loc.GetString("door-remote-no-power"), user, user);
            return;
        }

        var accessTarget = used;
        // This covers the accesses the REMOTE has, and is not effected by the user's ID card.
        if (entity.Comp.IncludeUserAccess) // Allows some door remotes to inherit the user's access.
        {
            accessTarget = user;
            // This covers the accesses the USER has, which always includes the remote's access since holding a remote acts like holding an ID card.
        }

        // Only let remote work on doors that have AccessReader; otherwise, it works on anything with a Door component (curtains, fence gates, etc)
        if (TryComp<AccessReaderComponent>(target, out var accessComponent) && _tagSystem.HasTag(target.Value, entity.Comp.TargetTag))
        {
            // Has an access reader component. Check access.
            if (!_doorSystem.HasAccess(target.Value, accessTarget, doorComp, accessComponent))
            {
                if (isAirlock)
                    _doorSystem.Deny(target.Value, doorComp, user: user, predicted: true);

                _popup.PopupEntity(Loc.GetString("door-remote-denied"), user, user);
                return;
            }
        }
        // Unless allowed to bypass by the flag on the component.
        else if (entity.Comp.RequireTagWhitelist)
            return;

        switch (entity.Comp.Mode)
        {
            case OperatingMode.OpenClose:
                if (_doorSystem.TryToggleDoor(target.Value, doorComp, user: user, predicted: true))
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(user):player} used {ToPrettyString(used)} on {ToPrettyString(target.Value)}: {doorComp.State}");
                break;
            case OperatingMode.ToggleBolts:
                if (TryComp<DoorBoltComponent>(target, out var boltsComp))
                {
                    if (!boltsComp.BoltWireCut)
                    {
                        _doorSystem.SetBoltsDown((target.Value, boltsComp), !boltsComp.BoltsDown, user: user, predicted: true);
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Medium,
                            $"{ToPrettyString(user):player} used {ToPrettyString(used)} on {ToPrettyString(target.Value)} to {(boltsComp.BoltsDown ? "" : "un")}bolt it");
                    }
                }

                break;
            case OperatingMode.ToggleEmergencyAccess:
                if (airlockComp != null)
                {
                    _airlock.SetEmergencyAccess((target.Value, airlockComp), !airlockComp.EmergencyAccess, user: user, predicted: true);
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(user):player} used {ToPrettyString(used)} on {ToPrettyString(target.Value)} to set emergency access {(airlockComp.EmergencyAccess ? "on" : "off")}");
                }

                break;
            case OperatingMode.ToggleOvercharge:
                if (TryComp<ElectrifiedComponent>(target, out var eletrifiedComp))
                {
                    _electrify.SetElectrified((target.Value, eletrifiedComp), !eletrifiedComp.Enabled);
                    var soundToPlay = eletrifiedComp.Enabled
                        ? eletrifiedComp.AirlockElectrifyEnabled
                        : eletrifiedComp.AirlockElectrifyDisabled;
                    _audio.PlayLocal(soundToPlay, target.Value, user);
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(user):player} used {ToPrettyString(used)} on {ToPrettyString(target.Value)} to {(eletrifiedComp.Enabled ? "" : "un")}electrify it");
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
