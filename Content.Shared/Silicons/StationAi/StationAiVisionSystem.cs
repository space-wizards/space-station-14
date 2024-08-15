using Content.Shared.StationAi;
using Robust.Shared.Map.Components;

namespace Content.Shared.Silicons.StationAi;

public sealed class StationAiVisionSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public void GetView(Entity<MapGridComponent> grid, Box2 worldAabb, List<Entity<StationAiVisionComponent>> seeds, HashSet<Vector2i> tiles)
    {
        var tileEnumerator = _maps.GetTilesEnumerator(grid, grid, worldAabb, ignoreEmpty: false);
        /*

        // Get what tiles are blocked up-front.
        while (tileEnumerator.MoveNext(out var tileRef))
        {
            var tileBounds = _lookup.GetLocalBounds(tileRef.GridIndices, grid.Comp.TileSize).Enlarged(-0.05f);

            occluders.Clear();
            lookups.GetLocalEntitiesIntersecting(gridUid, tileBounds, occluders, LookupFlags.Static);

            if (occluders.Count > 0)
            {
                opaque.Add(tileRef.GridIndices);
            }
            else
            {
                cleared.Add(tileRef.GridIndices);
            }
        }

        */
    }
}
