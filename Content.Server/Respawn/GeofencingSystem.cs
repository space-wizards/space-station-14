using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Respawn;
using Content.Server.Shuttles.Systems;
using Content.Shared.Database;
using Content.Shared.Geofencing;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using System.Numerics;
using Microsoft.CodeAnalysis.Host;

namespace Content.Server.Geofencing;

public sealed class GeofencingSystem : EntitySystem
{

    [Dependency] private readonly IAdminLogManager _adminlogs = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SpecialRespawnSystem _respawnSys = default!;


    static private float _updateRate = 2; //How frequently to do our geofencing queries for perf reasons.
    private TimeSpan _nextUpdate = TimeSpan.FromSeconds(_updateRate);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GeofencingComponent, MapInitEvent>(OnMapInit);

    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_nextUpdate > _timing.CurTime)
            return;

        _nextUpdate = _nextUpdate + TimeSpan.FromSeconds(_updateRate);

        var fencequery = EntityQueryEnumerator<GeofencingComponent>();
        while (fencequery.MoveNext(out var uid, out var geofence))
        {
            var fencedent = (uid, geofence);

            if (!geofence.Geofence)
                continue;

            if (!FencedIsAway(fencedent))
            {
                geofence.LeftStation = false;
                geofence.LeftStationWhen = TimeSpan.MaxValue;
                continue;
            }

            if (geofence.LeftStation && geofence.LeftStationWhen + geofence.OffStationTolerance <= _timing.CurTime)
            {
                ReturnFencedToOrigin(fencedent);
            }
            else if (geofence.LeftStation)
            {
                if (geofence.LastPopup + TimeSpan.FromSeconds(30) <= _timing.CurTime)
                {
                    var diskwarning = Loc.GetString("geofence-feelin-weird", ("entity", uid));
                    _popups.PopupEntity(diskwarning, uid);
                    geofence.LastPopup = _timing.CurTime;
                }
                // else do nothing and wait for next update
            }
            else
            {
                geofence.LeftStation = true;
                geofence.LeftStationWhen = _timing.CurTime;
            }
        }
    }

    /// <summary>
    /// Checks if the fenced entity has been put somewhere unreasonably unaccesible.
    /// </summary>
    private bool FencedIsAway(Entity<GeofencingComponent> fencedent)
    {
        if (_emergencyShuttle.EmergencyShuttleArrived)
            return false;

        var ev = new TryGeofenceAttemptEvent();
        RaiseLocalEvent(fencedent.Owner, ev);
        if (ev.Cancelled)
            return false;

        var transform = Transform(fencedent.Owner);
        if (fencedent.Comp.OriginGrid != null && fencedent.Comp.OriginGrid != transform.GridUid)
            return true;
        if (fencedent.Comp.OriginMap != null && fencedent.Comp.OriginMap != transform.MapUid)
            return true;

        return false;
    }

    /// <summary>
    /// Attempt to send the geofenced entity to whatever station it came from.
    /// </summary>
    /// <param name="fencedent"></param>
    private void ReturnFencedToOrigin(Entity<GeofencingComponent> fencedent)
    {
        // some flare to fuck with the entity holder
        var fencedgoodbye = Loc.GetString("geofence-poof", ("entity", fencedent.Owner));
        _popups.PopupEntity(fencedgoodbye, fencedent.Owner);

        if (fencedent.Comp.OriginGrid != null)
        {
            // tp to OriginGrid
            var gridtransform = Transform(fencedent.Comp.OriginGrid.Value);
            if (gridtransform.MapUid == null)
            {
                Log.Debug($"Issue when geofencing {ToPrettyString(fencedent.Owner)}, origin grid {fencedent.Comp.OriginGrid} is on a null map!");
                return;
            }

            if (!_respawnSys.TryFindRandomTile(fencedent.Comp.OriginGrid.Value, gridtransform.MapUid.Value, 3, out var target))
            {
                Log.Debug($"No random tile found for {fencedent.Comp.OriginGrid} when geofencing {ToPrettyString(fencedent.Owner)}");
                return;
            }

            _adminlogs.Add(LogType.Teleport, LogImpact.Medium, $"The {ToPrettyString(fencedent.Owner)} has been teleported to its origin grid {ToPrettyString(fencedent.Comp.OriginGrid)}.");
            _transform.SetCoordinates(fencedent.Owner, target);
        }
        else if (fencedent.Comp.OriginMap != null)
        {
            // tp to OriginMap if no grid
            _adminlogs.Add(LogType.Teleport, LogImpact.Medium, $"The {ToPrettyString(fencedent.Owner)} has been teleported to its origin map {ToPrettyString(fencedent.Comp.OriginMap)}.");
            _transform.SetCoordinates(fencedent.Owner, new EntityCoordinates(fencedent.Comp.OriginMap.Value, Vector2.Zero));
        }
        else
        {
            fencedent.Comp.Geofence = false;
            return;
        }

        // some flare for where the entity appears
        SpawnNextToOrDrop(fencedent.Comp.TeleportFlare, fencedent.Owner);
    }

    private void OnMapInit(EntityUid uid, GeofencingComponent geofence, MapInitEvent args)
    {
        var transform = Transform(uid);
        geofence.OriginMap = transform.MapUid;
        geofence.OriginGrid = transform.GridUid;
    }

    /// <summary>
    /// Raised at an entity to see if something is keeping it from being geofenced.
    /// </summary>
    public sealed class TryGeofenceAttemptEvent : CancellableEntityEventArgs
    {

    }
}
