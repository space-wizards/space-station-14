using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    private const bool SimplifiedSameGrid = true;

    private void UpdateGridcast()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var sourceQuery = EntityQuery<RadiationSourceComponent, TransformComponent>();
        var destQuery = EntityQuery<RadiationReceiverComponent, TransformComponent>();

        var rays = new List<RadiationRay>();
        foreach (var (source, sourceTrs) in sourceQuery)
        {
            foreach (var (dest, destTrs) in destQuery)
            {
                var ray = Irradiate(sourceTrs, destTrs, source.RadsPerSecond);
                if (ray != null)
                    rays.Add(ray);
            }
        }

        Logger.Info($"Gridcast radiation {stopwatch.Elapsed.TotalMilliseconds}ms");

        RaiseNetworkEvent(new RadiationGridcastUpdate(rays));
    }

    private RadiationRay? Irradiate(TransformComponent sourceTrs, TransformComponent destTrs,
        float incomingRads)
    {
        // lets first check that source and destination on the same map
        if (sourceTrs.MapID != destTrs.MapID)
            return null;
        var mapId = sourceTrs.MapID;

        // get direction from rad source to destination and its distance
        var sourceWorldPos = sourceTrs.WorldPosition;
        var destWorldPos = destTrs.WorldPosition;
        var dir = destWorldPos - sourceWorldPos;
        var dist = dir.Length;

        // will it even reach destination considering distance penalty
        var rads = dist > 1f ? incomingRads / dist : incomingRads;
        if (rads <= MinRads)
            return null;

        // if source and destination on the same grid it's possible that
        // between them can be another grid (ie. shuttle in center of donut station)
        // however we can do simplification and ignore that case
        if (SimplifiedSameGrid && sourceTrs.GridUid != null && sourceTrs.GridUid == destTrs.GridUid)
        {
            return Gridcast(sourceTrs.GridUid.Value, sourceWorldPos, destWorldPos, rads);
        }

        // lets check how many grids are between source and destination
        // do a raycast to get list of all grids that this rad is going to visit
        // it should be pretty cheap because we do it only on grid bounds
        var ray = new Ray(sourceWorldPos, dir.Normalized);
        var raycastResults = _mapManager.Raycast(mapId, ray, dist, false).ToList();
        var gridsCount = raycastResults.Count;

        if (gridsCount == 0)
        {
            // no grids found - so no blockers (just distance penalty)
            return new RadiationRay
            {
                Source = sourceWorldPos,
                Destination = destWorldPos,
                Rads = rads
            };
        }
        else if (gridsCount == 1)
        {
            // one grid found - use it for gridcast
            return Gridcast(raycastResults[0].HitEntity, sourceWorldPos, destWorldPos, rads);
        }
        else
        {
            // more than one grid - fallback to raycast
            return Raycast(mapId, ray, dist, rads);
        }
    }

    private RadiationRay Gridcast(EntityUid gridUid, Vector2 sourceWorld, Vector2 destWorld,
        float incomingRads)
    {
        var visitedTiles = new List<(Vector2i, float)>();
        var radRay = new RadiationRay
        {
            Source = sourceWorld,
            Destination = destWorld,
            Rads = incomingRads,
            Grid = gridUid,
            VisitedTiles = visitedTiles
        };

        if (!_resistancePerTile.TryGetValue(gridUid, out var resistanceMap))
            return radRay;

        if (!TryComp(gridUid, out IMapGridComponent? grid))
            return radRay;
        var sourceGridPos = grid.Grid.TileIndicesFor(sourceWorld);
        var destGridPos = grid.Grid.TileIndicesFor(destWorld);
        var line = Line(sourceGridPos.X, sourceGridPos.Y, destGridPos.X, destGridPos.Y);

        foreach (var point in line)
        {
            if (resistanceMap.TryGetValue(point, out var resData))
                radRay.Rads -= resData.Tolerance[0];
            visitedTiles.Add((point, radRay.Rads));

            if (radRay.Rads <= MinRads)
                return radRay;
        }

        return radRay;
    }

    private RadiationRay Raycast(MapId mapId, Ray ray, float distance, float incomingRads)
    {
        var blockers = new List<(Vector2, float)>();
        var radRay = new RadiationRay
        {
            Source = ray.Position,
            Destination = ray.Position + ray.Direction *distance,
            Rads = incomingRads,
            Blockers = blockers
        };

        // do raycast to the physics
        var colRay = new CollisionRay(ray.Position, ray.Direction, (int) CollisionGroup.Impassable);
        var results = _physicsSystem.IntersectRay(mapId, colRay, distance, returnOnFirstHit: false);

        var rads = incomingRads;
        var blockerQuery = GetEntityQuery<RadiationBlockerComponent>();
        foreach (var obstacle in results)
        {
            if (!blockerQuery.TryGetComponent(obstacle.HitEntity, out var blocker))
                continue;

            rads -= blocker.RadResistance;
            blockers.Add((obstacle.HitPos, rads));
        }

        return radRay;
    }


    // https://stackoverflow.com/questions/11678693/all-cases-covered-bresenhams-line-algorithm
    // need to rewrite to make any sense
    private IEnumerable<Vector2i> Line(int x, int y, int x2, int y2)
    {
        var w = x2 - x;
        var h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0)
            dx1 = -1;
        else if (w > 0)
            dx1 = 1;
        if (h < 0)
            dy1 = -1;
        else if (h > 0)
            dy1 = 1;
        if (w < 0)
            dx2 = -1;
        else if (w > 0)
            dx2 = 1;


        var longest = Math.Abs(w);
        var shortest = Math.Abs(h);
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            if (h < 0)
                dy2 = -1;
            else if (h > 0)
                dy2 = 1;
            dx2 = 0;
        }

        var numerator = longest >> 1;
        for (var i = 0; i <= longest; i++)
        {
            yield return new Vector2i(x, y);
            numerator += shortest;
            if (!(numerator < longest))
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
