using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Shuttles.Events;
using Robust.Shared.Map.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleConsoleSystem
{
    private void OnDestinationMessage(EntityUid uid, ShuttleConsoleComponent component, ShuttleConsoleFTLRequestMessage args)
    {
        /*
         * TODO:
         * FTL position request -> locked in the whole time.
         * FTL dock request -> Picks a random dock when arriving stage, if not possible then mishap and land nearby.
         * Need some flag to force it or not.
         * FTL dock request only valid for arrivals and shit.
         * Make FTL boundaries actually work with grid queries and show dotted line update.
         * Need to make boundaries not show on IFF entities.
         * Maybe need to iterate every grid on map and determine
         */

        var mapUid = _mapManager.GetMapEntityId(args.Coordinates.MapId);

        if (!Exists(mapUid))
        {
            return;
        }

        if (!TryComp<FTLDestinationComponent>(mapUid, out var dest))
        {
            return;
        }

        if (!dest.Enabled)
            return;

        EntityUid? entity = uid;

        var getShuttleEv = new ConsoleShuttleEvent
        {
            Console = uid,
        };

        RaiseLocalEvent(entity.Value, ref getShuttleEv);
        entity = getShuttleEv.Console;

        if (!TryComp<TransformComponent>(entity, out var xform) ||
            !TryComp<ShuttleComponent>(xform.GridUid, out var shuttle))
        {
            return;
        }

        if (dest.Whitelist?.IsValid(entity.Value, EntityManager) == false &&
            dest.Whitelist?.IsValid(xform.GridUid.Value, EntityManager) == false)
        {
            return;
        }

        var shuttleUid = xform.GridUid.Value;

        if (HasComp<FTLComponent>(shuttleUid))
        {
            _popup.PopupCursor(Loc.GetString("shuttle-console-in-ftl"), args.Session);
            return;
        }

        if (!_shuttle.CanFTL(xform.GridUid, out var reason))
        {
            _popup.PopupCursor(reason, args.Session);
            return;
        }

        var dock = HasComp<MapComponent>(destination) && HasComp<MapGridComponent>(destination);
        var tagEv = new FTLTagEvent();
        RaiseLocalEvent(xform.GridUid.Value, ref tagEv);

        var ev = new ShuttleConsoleFTLTravelStartEvent(uid);
        RaiseLocalEvent(ref ev);

        _shuttle.FTLTravel(xform.GridUid.Value, shuttle, destination, dock: dock, priorityTag: tagEv.Tag);
    }
}
