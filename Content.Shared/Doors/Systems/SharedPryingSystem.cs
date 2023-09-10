using Content.Shared.Doors.Prying.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Tools.Components;

namespace Content.Shared.Doors.Prying.Systems;
public abstract class SharedDoorPryingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, DoorPryDoAfterEvent>(OnDoAfter);
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

        if (comp.NeedsComponent)
        {
            ToolComponent? tComp = null;
            if (!Resolve(tool, ref tComp) || !tComp.Qualities.Contains("Prying"))
                return false;
        }

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

        if (!TryComp<PryUnpoweredComponent>(target, out _))
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
        var doAfterArgs = new DoAfterArgs(user, TimeSpan.FromSeconds(door.PryTime * modEv.PryTimeModifier / toolModifier), new DoorPryDoAfterEvent(), target, target, tool)
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

    protected virtual void OnDoAfter(EntityUid uid, DoorComponent door, DoorPryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Target is null)
            return;

        DoorPryingComponent? comp = null;

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
public sealed partial class DoorPryDoAfterEvent : SimpleDoAfterEvent
{
}
