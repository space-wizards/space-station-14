using Content.Shared.Doors.Prying.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;

namespace Content.Shared.Doors.Prying.Systems;
public sealed class DoorPryingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

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
            _adminLog.Add(LogType.Action, LogImpact.Low, $"darn");
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
        var modEv = new PryTimeModifierEvent(user);

        RaiseLocalEvent(target, modEv, false);
        var doAfterArgs = new DoAfterArgs(user, door.PryTime * modEv.Modifier / toolModifier, new PryDoAfterEvent(), target, target)
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

}

[Serializable, NetSerializable]
public sealed class PryDoAfterEvent : SimpleDoAfterEvent
{
}
