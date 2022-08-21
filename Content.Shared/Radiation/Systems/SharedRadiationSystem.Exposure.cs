using Content.Shared.Radiation.Components;
using Robust.Shared.Map;

namespace Content.Shared.Radiation.Systems;

public partial class SharedRadiationSystem
{
    public Dictionary<EntityUid, Dictionary<Vector2i, float>> _radiationMap = new();

    private void UpdateRadSources()
    {
        foreach (var comp in EntityManager.EntityQuery<RadiationSourceComponent>())
        {
            var ent = comp.Owner;
            var cords = Transform(ent).MapPosition;
            CalculateRadiationMap(cords, comp.RadsPerSecond);
        }
    }

    public void CalculateRadiationMap(MapCoordinates epicenter, float radsPerSecond)
    {
        MapId = epicenter.MapId;

        Vector2i initialTile;
        if (_mapManager.TryFindGridAt(epicenter, out var candidateGrid) &&
            candidateGrid.TryGetTileRef(candidateGrid.WorldToTile(epicenter.Position), out var tileRef) )
        {
            gridUid = tileRef.GridUid;
            initialTile = tileRef.GridIndices;
        }
        else
        {
            return;
        }

        var query = GetEntityQuery<RadiationBlockerComponent>();

        visitedTiles.Clear();
        var visitNext = new Queue<(Vector2i, float)>();
        visitNext.Enqueue((initialTile, radsPerSecond));

        do
        {
            var (current, incomingRads) = visitNext.Dequeue();
            if (visitedTiles.ContainsKey(current) || incomingRads <= 0f)
                continue;

            visitedTiles.Add(current, incomingRads);

            // here is material absorption
            var nextRad = incomingRads;
            var ents = candidateGrid.GetAnchoredEntities(current);
            foreach (var uid in ents)
            {
                if (!query.TryGetComponent(uid, out var blocker))
                    continue;
                nextRad -= blocker.RadResistance;
            }


            // and also remove by distance
            nextRad -= 1f;
            // if no radiation power left - don't propagate further
            if (nextRad <= 0)
                continue;

            foreach (var dir in _directions)
            {
                var next = current.Offset(dir);
                visitNext.Enqueue((next, nextRad));
            }

        } while (visitNext.Count != 0);
    }
}
