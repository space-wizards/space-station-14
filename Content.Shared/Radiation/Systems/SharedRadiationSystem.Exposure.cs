using Robust.Shared.Map;

namespace Content.Shared.Radiation.Systems;

public partial class SharedRadiationSystem
{
    private const float DistanceScore = 1f;

    public Dictionary<EntityUid, Dictionary<Vector2i, float>> _radiationMap = new();

    private readonly Direction[] _directions =
    {
        Direction.North, Direction.South, Direction.East, Direction.West,
    };

    private void UpdateRadSources()
    {
        foreach (var (_, map) in _radiationMap)
        {
            map.Clear();
        }

        foreach (var comp in EntityManager.EntityQuery<RadiationSourceComponent>())
        {
            var ent = comp.Owner;
            var cords = Transform(ent).MapPosition;
            CalculateRadiationMap(cords, comp.RadsPerSecond);
        }
    }

    public void CalculateRadiationMap(MapCoordinates epicenter, float radsPerSecond)
    {
        if (!_mapManager.TryFindGridAt(epicenter, out var candidateGrid) ||
            !candidateGrid.TryGetTileRef(candidateGrid.WorldToTile(epicenter.Position), out var tileRef))
        {
            return;
        }

        var gridUid = tileRef.GridUid;
        if (!_radiationMap.ContainsKey(gridUid))
        {
            _radiationMap.Add(gridUid, new Dictionary<Vector2i, float>());
        }
        var map = _radiationMap[gridUid];

        _resistancePerTile.TryGetValue(gridUid, out var resistance);
        var initialTile = tileRef.GridIndices;

        var visitNext = new Queue<(Vector2i, float)>();
        var visitedTiles = new HashSet<Vector2i>();
        visitNext.Enqueue((initialTile, radsPerSecond));

        var counter = 0;
        do
        {
            counter++;

            var (current, incomingRads) = visitNext.Dequeue();
            if (visitedTiles.Contains(current))
                continue;
            visitedTiles.Add(current);

            if (map.ContainsKey(current))
                map[current] += incomingRads;
            else
                map[current] = incomingRads;

            // do resistance
            var nextRad = incomingRads;
            if (resistance != null && resistance.TryGetValue(current, out var res))
            {
                nextRad -= res;
            }

            // and also remove by distance
            nextRad -= DistanceScore;
            // if no radiation power left - don't propagate further
            if (nextRad <= 0)
                continue;

            foreach (var dir in _directions)
            {
                var next = current.Offset(dir);
                if (!visitedTiles.Contains(next))
                    visitNext.Enqueue((next, nextRad));
            }

        } while (visitNext.Count != 0);

        Logger.Debug($"Visited tiles {visitedTiles.Count}");
        Logger.Debug($"Cycle iterations {counter}");
    }
}
