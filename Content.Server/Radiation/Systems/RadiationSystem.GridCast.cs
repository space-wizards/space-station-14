using System.Linq;
using Content.Server.Radiation.Components;
using Content.Shared.Physics;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

// main algorithm that fire radiation rays to target
public partial class RadiationSystem
{
    private void UpdateGridcast()
    {
        // should we save debug information into rays?
        // if there is no debug sessions connected - just ignore it
        var saveVisitedTiles = _debugSessions.Count > 0;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var sources = EntityQuery<RadiationSourceComponent, TransformComponent>().ToArray();
        var destinations = EntityQuery<RadiationReceiverComponent, TransformComponent>().ToArray();
        var blockerQuery = GetEntityQuery<RadiationBlockerComponent>();
        var resistanceQuery = GetEntityQuery<RadiationGridResistanceComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();

        // trace all rays from rad source to rad receivers
        var rays = new List<RadiationRay>();
        var receivedRads = new List<(RadiationReceiverComponent, float)>();
        foreach (var (dest, destTrs) in destinations)
        {
            var destWorld = _transform.GetWorldPosition(destTrs, transformQuery);

            var rads = 0f;
            foreach (var (source, sourceTrs) in sources)
            {
                // send ray towards destination entity
                var ray = Irradiate(sourceTrs.Owner, sourceTrs, destTrs.Owner, destTrs, destWorld,
                    source.Intensity, source.Slope, saveVisitedTiles,
                    blockerQuery, resistanceQuery, transformQuery);
                if (ray == null)
                    continue;

                // save ray for debug
                rays.Add(ray);

                // add rads to total rad exposure
                if (ray.ReachedDestination)
                    rads += ray.Rads;
            }

            receivedRads.Add((dest, rads));
        }

        // update information for debug overlay
        var elapsedTime = stopwatch.Elapsed.TotalMilliseconds;
        var totalSources = sources.Length;
        var totalReceivers = destinations.Length;
        UpdateGridcastDebugOverlay(elapsedTime, totalSources, totalReceivers, rays);

        // send rads to each entity
        foreach (var (receiver, rads) in receivedRads)
        {
            // update radiation value of receiver
            // if no radiation rays reached target, that will set it to 0
            receiver.CurrentRadiation = rads;

            // also send an event with combination of total rad
            if (rads > 0)
                IrradiateEntity(receiver.Owner, rads,GridcastUpdateRate);
        }
    }

    private RadiationRay? Irradiate(EntityUid sourceUid, TransformComponent sourceTrs,
        EntityUid destUid, TransformComponent destTrs, Vector2 destWorld,
        float incomingRads, float slope, bool saveVisitedTiles,
        EntityQuery<RadiationBlockerComponent> blockerQuery,
        EntityQuery<RadiationGridResistanceComponent> resistanceQuery,
        EntityQuery<TransformComponent> transformQuery)
    {
        // lets first check that source and destination on the same map
        if (sourceTrs.MapID != destTrs.MapID)
            return null;
        var mapId = sourceTrs.MapID;

        // get direction from rad source to destination and its distance
        var sourceWorld = _transform.GetWorldPosition(sourceTrs, transformQuery);
        var dir = destWorld - sourceWorld;
        var dist = dir.Length;

        // check if receiver is too far away
        if (dist > GridcastMaxDistance)
            return null;
        // will it even reach destination considering distance penalty
        var rads = incomingRads - slope * dist;
        if (rads <= MinIntensity)
            return null;

        // create a new radiation ray from source to destination
        // at first we assume that it doesn't hit any radiation blockers
        // and has only distance penalty
        var ray = new RadiationRay(mapId, sourceUid, sourceWorld, destUid, destWorld, rads);

        // if source and destination on the same grid it's possible that
        // between them can be another grid (ie. shuttle in center of donut station)
        // however we can do simplification and ignore that case
        if (GridcastSimplifiedSameGrid && sourceTrs.GridUid != null && sourceTrs.GridUid == destTrs.GridUid)
        {
            // todo: entity queries doesn't support interface - use it when IMapGridComponent will be removed
            if (!TryComp(sourceTrs.GridUid.Value, out IMapGridComponent? gridComponent))
                return ray;
            return Gridcast(gridComponent.Grid, ray, saveVisitedTiles, resistanceQuery);
        }

        // lets check how many grids are between source and destination
        // do a box intersection test between target and destination
        // it's not very precise, but really cheap
        var box = Box2.FromTwoPoints(sourceWorld, destWorld);
        var grids = _mapManager.FindGridsIntersecting(mapId, box, true);

        // gridcast through each grid and try to hit some radiation blockers
        // the ray will be updated with each grid that has some blockers
        foreach (var grid in grids)
        {
            ray = Gridcast(grid, ray, saveVisitedTiles, resistanceQuery);

            // looks like last grid blocked all radiation
            // we can return right now
            if (ray.Rads <= 0)
                return ray;
        }

        return ray;
    }

    private RadiationRay Gridcast(IMapGrid grid, RadiationRay ray, bool saveVisitedTiles,
        EntityQuery<RadiationGridResistanceComponent> resistanceQuery)
    {
        var blockers = new List<(Vector2, float)>();

        // if grid doesn't have resistance map just apply distance penalty
        var gridUid = grid.GridEntityId;
        if (!resistanceQuery.TryGetComponent(gridUid, out var resistance))
            return ray;
        var resistanceMap = resistance.ResistancePerTile;

        // get coordinate of source and destination in grid coordinates
        var sourceGrid = grid.TileIndicesFor(ray.Source);
        var destGrid = grid.TileIndicesFor(ray.Destination);

        // iterate tiles in grid line from source to destination
        var line = Line(sourceGrid.X, sourceGrid.Y, destGrid.X, destGrid.Y);
        foreach (var point in line)
        {
            if (!resistanceMap.TryGetValue(point, out var resData))
                continue;
            ray.Rads -= resData;

            // save data for debug
            if (saveVisitedTiles)
            {
                var worldPos = grid.GridTileToWorldPos(point);
                blockers.Add((worldPos, ray.Rads));
            }

            // no intensity left after blocker
            if (ray.Rads <= MinIntensity)
            {
                ray.Rads = 0;
                break;
            }
        }

        // save data for debug if needed
        if (saveVisitedTiles && blockers.Count > 0)
            ray.Blockers = blockers;

        return ray;
    }

    // bresenhams line algorithm
    // this is slightly rewritten version of code bellow
    // https://stackoverflow.com/questions/11678693/all-cases-covered-bresenhams-line-algorithm
    private IEnumerable<Vector2i> Line(int x, int y, int x2, int y2)
    {
        var w = x2 - x;
        var h = y2 - y;

        var dx1 = Math.Sign(w);
        var dy1 = Math.Sign(h);
        var dx2 = Math.Sign(w);
        var dy2 = 0;

        var longest = Math.Abs(w);
        var shortest = Math.Abs(h);
        if (longest <= shortest)
        {
            (longest, shortest) = (shortest, longest);
            dx2 = 0;
            dy2 = Math.Sign(h);
        }

        var numerator = longest / 2;
        for (var i = 0; i <= longest; i++)
        {
            yield return new Vector2i(x, y);
            numerator += shortest;
            if (numerator >= longest)
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }
}
