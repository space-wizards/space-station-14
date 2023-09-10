using Content.Shared.Doors.Prying.Systems;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Prying.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Doors.Systems;

namespace Content.Server.Doors.Prying.Systems;

// Why does this exist you may be asking? Because if you call the
// SharedDoorSystem in the server DoorSystem when it attempts to pry open or
// closed a door it will run the open method of the door system twice (once from the
// shared system and once from the server system) duplicating the door opening and closing sound.
public sealed class DoorPryingSystem : SharedDoorPryingSystem
{

    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;

    protected override void OnDoAfter(EntityUid uid, DoorComponent door, DoorPryDoAfterEvent args)
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
