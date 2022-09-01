using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Events;
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
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var sources = EntityQuery<RadiationSourceComponent, TransformComponent>().ToArray();
        var destinations = EntityQuery<RadiationReceiverComponent, TransformComponent>().ToArray();
        var blockerQuery = GetEntityQuery<RadiationBlockerComponent>();

        // trace all rays from rad source to rad receivers
        var rays = new List<RadiationRay>();
        foreach (var (source, sourceTrs) in sources)
        {
            foreach (var (_, destTrs) in destinations)
            {
                var ray = Irradiate(sourceTrs.Owner, sourceTrs, destTrs.Owner, destTrs,
                    source.RadsPerSecond, source.Slope, blockerQuery);
                if (ray != null)
                    rays.Add(ray);
            }
        }

        // update information for debug overlay
        var elapsedTime = stopwatch.Elapsed.TotalMilliseconds;
        var totalSources = sources.Length;
        var totalReceivers = destinations.Length;
        var ev = new OnRadiationOverlayUpdateEvent(elapsedTime, totalSources, totalReceivers, rays);
        UpdateDebugView(ev);

        // send rads for each entity
        foreach (var ray in rays)
        {
            if (ray.Rads <= 0)
                continue;
            IrradiateEntity(ray.DestinationUid, ray.Rads, GridcastUpdateRate);
        }
    }

    private RadiationRay? Irradiate(EntityUid sourceUid, TransformComponent sourceTrs,
        EntityUid destUid, TransformComponent destTrs,
        float incomingRads, float slope,
        EntityQuery<RadiationBlockerComponent> blockerQuery)
    {
        // lets first check that source and destination on the same map
        if (sourceTrs.MapID != destTrs.MapID)
            return null;
        var mapId = sourceTrs.MapID;

        // get direction from rad source to destination and its distance
        var sourceWorld = sourceTrs.WorldPosition;
        var destWorld = destTrs.WorldPosition;
        var dir = destWorld - sourceWorld;
        var dist = dir.Length;

        // will it even reach destination considering distance penalty
        var slopeDist = slope * dist;
        var rads = slopeDist > 1f ? incomingRads / slopeDist : incomingRads;
        if (rads <= MinIntensity)
            return null;

        // if source and destination on the same grid it's possible that
        // between them can be another grid (ie. shuttle in center of donut station)
        // however we can do simplification and ignore that case
        if (GridcastSimplifiedSameGrid && sourceTrs.GridUid != null && sourceTrs.GridUid == destTrs.GridUid)
        {
            return Gridcast(mapId, sourceTrs.GridUid.Value, sourceUid, destUid,
                sourceWorld, destWorld, rads);
        }

        // lets check how many grids are between source and destination
        // do a box intersection test between target and destination
        // it's not very precise, but really cheap
        var box = Box2.FromTwoPoints(sourceWorld, destWorld);
        var grids = _mapManager.FindGridsIntersecting(mapId, box, true);

        // we are only interested in grids that has some radiation blockers
        var resGrids = grids.Where(grid => _resistancePerTile.ContainsKey(grid.GridEntityId)).ToArray();
        var resGridsCount = resGrids.Length;

        if (resGridsCount == 0)
        {
            // no grids found - so no blockers (just distance penalty)
            return new RadiationRay(mapId, sourceUid,sourceWorld,
                destUid,destWorld, rads);
        }
        else if (resGridsCount == 1)
        {
            // one grid found - use it for gridcast
            return Gridcast(mapId, resGrids[0].GridEntityId, sourceUid, destUid,
                sourceWorld, destWorld, rads);
        }
        else
        {
            // more than one grid - fallback to raycast
            return Raycast(mapId, sourceUid, destUid, sourceWorld, destWorld,
                dir.Normalized, dist, rads, blockerQuery);
        }
    }

    private RadiationRay Gridcast(MapId mapId, EntityUid gridUid, EntityUid sourceUid, EntityUid destUid,
        Vector2 sourceWorld, Vector2 destWorld, float incomingRads)
    {
        var visitedTiles = new List<(Vector2i, float?)>();
        var radRay = new RadiationRay(mapId, sourceUid,sourceWorld,
            destUid,destWorld, incomingRads)
        {
            Grid = gridUid,
            VisitedTiles = visitedTiles
        };

        // if grid doesn't have resistance map just apply distance penalty
        if (!_resistancePerTile.TryGetValue(gridUid, out var resistanceMap))
            return radRay;

        // get coordinate of source and destination in grid coordinates
        // todo: entity queries doesn't support interface - use it when IMapGridComponent will be removed
        if (!TryComp(gridUid, out IMapGridComponent? grid))
            return radRay;
        var sourceGrid = grid.Grid.TileIndicesFor(sourceWorld);
        var destGrid = grid.Grid.TileIndicesFor(destWorld);

        // iterate tiles in grid line from source to destination
        var line = Line(sourceGrid.X, sourceGrid.Y, destGrid.X, destGrid.Y);
        foreach (var point in line)
        {
            if (resistanceMap.TryGetValue(point, out var resData))
            {
                radRay.Rads -= resData;
                visitedTiles.Add((point, radRay.Rads));
            }
            else
            {
                visitedTiles.Add((point, null));
            }

            // no intensity left after blocker
            if (radRay.Rads <= MinIntensity)
            {
                radRay.Rads = 0;
                return radRay;
            }
        }

        return radRay;
    }

    private RadiationRay Raycast(MapId mapId, EntityUid sourceUid, EntityUid destUid,
        Vector2 sourceWorld, Vector2 destWorld, Vector2 dir, float distance, float incomingRads,
        EntityQuery<RadiationBlockerComponent> blockerQuery)
    {
        var blockers = new List<(Vector2, float)>();
        var radRay = new RadiationRay(mapId, sourceUid, sourceWorld,
            destUid, destWorld, incomingRads)
        {
            Blockers = blockers
        };

        var colRay = new CollisionRay(sourceWorld, dir, (int) CollisionGroup.Impassable);
        var results = _physicsSystem.IntersectRay(mapId, colRay, distance, returnOnFirstHit: false);

        foreach (var obstacle in results)
        {
            if (!blockerQuery.TryGetComponent(obstacle.HitEntity, out var blocker))
                continue;

            radRay.Rads -= blocker.RadResistance;
            blockers.Add((obstacle.HitPos, radRay.Rads));

            if (radRay.Rads <= MinIntensity)
            {
                radRay.Rads = 0;
                return radRay;
            }
        }

        return radRay;
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
