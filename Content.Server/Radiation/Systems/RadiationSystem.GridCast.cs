using Content.Server.FloodFill;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    private void UpdateGridcast()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var sourceQuery = EntityQuery<RadiationSourceComponent, TransformComponent>();
        var destQuery = EntityQuery<RadiationReceiverComponent, TransformComponent>();

        var linesDict = new Dictionary<EntityUid, List<(List<Vector2i>, float)>>();
        foreach (var (source, sourceTrs) in sourceQuery)
        {
            if (sourceTrs.GridUid == null || !TryComp(sourceTrs.GridUid, out IMapGridComponent? grid))
                continue;

            var gridUid = sourceTrs.GridUid.Value;
            if (!linesDict.ContainsKey(gridUid))
                linesDict.Add(sourceTrs.GridUid.Value, new());
            var lines = linesDict[gridUid];

            var sourcePos = sourceTrs.WorldPosition;
            var sourceGridPos = grid.Grid.TileIndicesFor(sourceTrs.Coordinates);
            var resistanceMap = _resistancePerTile[gridUid];

            foreach (var (dest, destTrs) in destQuery)
            {
                var line = IrradiateLine(source, sourceGridPos, destTrs, resistanceMap);
                if (line != null)
                    lines.Add(line.Value);
            }
        }

        Logger.Info($"Gridcast radiation {stopwatch.Elapsed.TotalMilliseconds}ms");

        RaiseNetworkEvent(new RadiationGridcastUpdate(linesDict));
    }

    private (List<Vector2i>, float)? IrradiateLine(RadiationSourceComponent source,
        Vector2i sourceGridPos, TransformComponent destTrs,
        Dictionary<Vector2i, TileData> resistanceMap)
    {
        if (destTrs.GridUid == null || !TryComp(destTrs.GridUid, out IMapGridComponent? destGrid))
            return null;
        var destGridPos = destGrid.Grid.TileIndicesFor(destTrs.Coordinates);

        var linesPoints = Line(sourceGridPos.X, sourceGridPos.Y,
            destGridPos.X, destGridPos.Y);

        var visitedPoints = new List<Vector2i>();
        var rads = source.RadsPerSecond;
        foreach (var point in linesPoints)
        {
            visitedPoints.Add(point);
            if (!resistanceMap.TryGetValue(point, out var resData))
                continue;

            rads -= resData.Tolerance[0];
            if (rads <= MinRads)
            {
                return (visitedPoints, 0f);
            }
        }

        return (visitedPoints, rads);
    }



    private IEnumerable<Vector2i> MultiGridLine(TransformComponent sourceTrs, TransformComponent destTrs)
    {
        // there is two cases
        // if source is located in space or if on a grid
        // lets start with a grid case and get grid position of source
        if (sourceTrs.GridUid == null || !TryComp(sourceTrs.GridUid, out IMapGridComponent? grid))
            yield break;
        var currentGrid = grid.Grid;
        var sourceGridPos = currentGrid.TileIndicesFor(sourceTrs.Coordinates);

        // get edges map
        if (!_floodFill._gridEdges.TryGetValue(currentGrid.GridEntityId, out var edges))
        {
            Logger.Error($"Grid {currentGrid.GridEntityId} doesn't have edges map");
            yield break;
        }

        // lets assume that target is placed on a same grid
        var destGridPos = currentGrid.TileIndicesFor(destTrs.WorldPosition);

        // lets start moving on the grid in direction of destination
        var lineEnumerator = Line(sourceGridPos.X, sourceGridPos.Y,
            destGridPos.X, destGridPos.Y);
        foreach (var pos in lineEnumerator)
        {
            // blah blah, we are moving and doing checks
            yield return pos;

            // if we started moving on a grid
            // and object is located on another grid
            // that means sooner or latter we will reach space by crossing grid edge


            if (edges.c)
        }


    }

    // https://stackoverflow.com/questions/11678693/all-cases-covered-bresenhams-line-algorithm
    // need to rewrite to make any sense
    public IEnumerable<Vector2i> Line(int x, int y, int x2, int y2)
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
