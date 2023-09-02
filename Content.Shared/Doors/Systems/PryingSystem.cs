using Content.Shared.Doors.Prying.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Shared.Doors.Prying.Systems;
public sealed class DoorPryingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorPryingComponent, DoorPryDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    ///     Pry open a door.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, DoorComponent door, out DoAfterId? id, EntityUid tool)
    {
        id = null;

        DoorPryingComponent? comp = null;
        if (!Resolve(tool, ref comp))
            return false;


        if (!CanPry(target, user, comp.PryPowered))
        {
            return true;
        }

        StartPry(target, user, door, tool, comp.SpeedModifier, out id);
        return true;
    }

    /// <summary>
    ///     Pry open a door.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, DoorComponent door, out DoAfterId? id)
    {
        id = null;

        if (!door.EasyPry)
            return false;

        if (!CanPry(target, user, false))
            return true;

        StartPry(target, user, door, null, 1.0f, out id);
        return true;
    }

    bool CanPry(EntityUid target, EntityUid user, bool pryPowered)
    {
        var canev = new BeforePryEvent(user, pryPowered);

        RaiseLocalEvent(target, canev, false);

        if (canev.Cancelled)
            return false;
        return true;
    }

    void StartPry(EntityUid target, EntityUid user, DoorComponent door, EntityUid? tool, float toolModifier, out DoAfterId? id)
    {
        var modEv = new DoorGetPryTimeModifierEvent(user);


        RaiseLocalEvent(target, modEv, false);
        var doAfterArgs = new DoAfterArgs(user, TimeSpan.FromSeconds(door.PryTime * modEv.PryTimeModifier / toolModifier), new DoorPryDoAfterEvent(), tool, target, tool)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
        };

        if (tool != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is using {ToPrettyString(tool.Value)} to pry {ToPrettyString(target)} while it is {door.State}");
        }
        else
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is prying {ToPrettyString(target)} while it is {door.State}");
        }
        _doAfterSystem.TryStartDoAfter(doAfterArgs, out id);
    }

    void OnDoAfter(EntityUid uid, DoorPryingComponent comp, DoorPryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Used is null)
            return;

        DoorComponent? door = null;
        if (!args.Target.HasValue || !Resolve(args.Target.Value, ref door))
            return;

        _audioSystem.PlayPredicted(comp.UseSound, args.Used.Value, args.User, comp.UseSound.Params.WithVariation(0.175f).AddVolume(-5f));

        if (door.State == DoorState.Closed)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User)} pried {ToPrettyString(args.Target.Value)} open");
            _doorSystem.StartOpening(args.Target.Value, door);
        }
        else if (door.State == DoorState.Open)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User)} pried {ToPrettyString(args.Target.Value)} closed");
            _doorSystem.StartClosing(args.Target.Value, door);
        }
    }
}

[Serializable, NetSerializable]
public sealed class DoorPryDoAfterEvent : SimpleDoAfterEvent
{
}
