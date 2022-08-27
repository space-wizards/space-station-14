using Content.Shared.Radiation.Components;
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

        var lines = new List<List<Vector2i>>();
        foreach (var (source, sourceTrs) in sourceQuery)
        {
            if (sourceTrs.GridUid == null || !TryComp(sourceTrs.GridUid, out IMapGridComponent? grid))
                continue;

            var sourcePos = sourceTrs.WorldPosition;
            var sourceGridPos = grid.Grid.TileIndicesFor(sourceTrs.Coordinates);

            foreach (var (dest, destTrs) in destQuery)
            {
                var dir = (destTrs.WorldPosition - sourcePos).Ceiled();
                var destGridPos = sourceGridPos + dir;

                var line = Line(sourceGridPos.X, sourceGridPos.Y,
                    destGridPos.X, destGridPos.Y);
                lines.Add(line);
            }
        }

        Logger.Info($"Gridcast radiation {stopwatch.Elapsed.TotalMilliseconds}ms");
    }

    public List<Vector2i> Line(int x, int y, int x2, int y2)
    {
        var list = new List<Vector2i>();

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
            list.Add(new Vector2i(x, y));
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

        return list;
    }
}
