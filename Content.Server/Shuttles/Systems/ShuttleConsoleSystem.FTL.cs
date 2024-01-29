using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Shuttles.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleConsoleSystem
{
    private void OnBeaconFTLMessage(Entity<ShuttleConsoleComponent> ent, ref ShuttleConsoleFTLBeaconMessage args)
    {
        var targetUid = GetEntity(args.Beacon);

        // Check target exists
        if (!Exists(targetUid) || !_xformQuery.TryGetComponent(targetUid, out var targetXform))
        {
            return;
        }

        var targetCoordinates = new EntityCoordinates(targetXform.MapUid!.Value, _transform.GetWorldPosition(targetUid));

        ConsoleFTL(ent, true, targetCoordinates, args.Angle, targetXform.MapID);
    }

    private void OnPositionFTLMessage(Entity<ShuttleConsoleComponent> entity, ShuttleConsoleFTLPositionMessage args)
    {
        var mapUid = _mapManager.GetMapEntityId(args.Coordinates.MapId);

        if (!Exists(mapUid))
        {
            return;
        }

        var targetCoordinates = new EntityCoordinates(mapUid, args.Coordinates.Position);
        ConsoleFTL(entity, false, targetCoordinates, args.Angle, args.Coordinates.MapId);
    }

    /// <summary>
    /// Handles shuttle console FTLs.
    /// </summary>
    private void ConsoleFTL(Entity<ShuttleConsoleComponent> ent, bool beacon, EntityCoordinates targetCoordinates, Angle targetAngle, MapId targetMap)
    {
        var consoleUid = GetDroneConsole(ent.Owner);

        if (consoleUid == null)
            return;

        var shuttleUid = _xformQuery.GetComponent(consoleUid.Value).GridUid;

        if (!TryComp(shuttleUid, out ShuttleComponent? shuttleComp))
            return;

        // Check shuttle can even FTL
        if (!_shuttle.CanFTL(shuttleUid.Value, out var reason))
        {
            // TODO: Session popup
            return;
        }

        // Check shuttle can FTL to this target.
        if (!_shuttle.CanFTLTo(shuttleUid.Value, targetMap, beacon))
        {
            return;
        }

        var tagEv = new FTLTagEvent();
        RaiseLocalEvent(shuttleUid.Value, ref tagEv);

        var ev = new ShuttleConsoleFTLTravelStartEvent(ent.Owner);
        RaiseLocalEvent(ref ev);

        _shuttle.FTLToCoordinates(shuttleUid.Value, shuttleComp, targetCoordinates, targetAngle);
    }
}
