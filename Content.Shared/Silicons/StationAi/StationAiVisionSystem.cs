using Content.Shared.NPC;
using Content.Shared.StationAi;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Shared.Silicons.StationAi;

public sealed class StationAiVisionSystem : EntitySystem
{
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    private SeedJob _seedJob;
    private ViewJob _job;

    private HashSet<Entity<OccluderComponent>> _occluders = new();
    private HashSet<Entity<StationAiVisionComponent>> _seeds = new();
    private HashSet<Vector2i> _viewportTiles = new();

    // Occupied tiles per-run.
    // For now it's only 1-grid supported but updating to TileRefs if required shouldn't be too hard.
    private readonly HashSet<Vector2i> _opaque = new();
    private readonly HashSet<Vector2i> _clear = new();

    public override void Initialize()
    {
        base.Initialize();

        _seedJob = new()
        {
            System = this,
        };

        _job = new ViewJob()
        {
            EntManager = EntityManager,
            Maps = _maps,
            System = this,
        };
    }

    /// <summary>
    /// Gets a byond-equivalent for tiles in the specified worldAABB.
    /// </summary>
    /// <param name="expansionSize">How much to expand the bounds before to find vision intersecting it. Makes this as small as you can.</param>
    public void GetView(Entity<MapGridComponent> grid, Box2Rotated worldBounds, HashSet<Vector2i> visibleTiles, float expansionSize = 7.5f)
    {
        _viewportTiles.Clear();
        _opaque.Clear();
        _clear.Clear();
        _seeds.Clear();
        var expandedBounds = worldBounds.Enlarged(expansionSize);

        // TODO: Would be nice to be able to run this while running the other stuff.
        _seedJob.Grid = grid;
        _seedJob.ExpandedBounds = expandedBounds;
        _parallel.ProcessNow(_seedJob);

        // Get viewport tiles
        var tileEnumerator = _maps.GetTilesEnumerator(grid, grid, worldBounds, ignoreEmpty: false);

        while (tileEnumerator.MoveNext(out var tileRef))
        {
            var tileBounds = _lookup.GetLocalBounds(tileRef.GridIndices, grid.Comp.TileSize).Enlarged(-0.05f);

            _occluders.Clear();
            _lookup.GetLocalEntitiesIntersecting(grid.Owner, tileBounds, _occluders, LookupFlags.Static);

            if (_occluders.Count > 0)
            {
                _opaque.Add(tileRef.GridIndices);
            }
            else
            {
                _clear.Add(tileRef.GridIndices);
            }

            _viewportTiles.Add(tileRef.GridIndices);
        }

        tileEnumerator = _maps.GetTilesEnumerator(grid, grid, expandedBounds, ignoreEmpty: false);

        // Get all other relevant tiles.
        while (tileEnumerator.MoveNext(out var tileRef))
        {
            if (_viewportTiles.Contains(tileRef.GridIndices))
                continue;

            var tileBounds = _lookup.GetLocalBounds(tileRef.GridIndices, grid.Comp.TileSize).Enlarged(-0.05f);

            _occluders.Clear();
            _lookup.GetLocalEntitiesIntersecting(grid.Owner, tileBounds, _occluders, LookupFlags.Static);

            if (_occluders.Count > 0)
            {
                _opaque.Add(tileRef.GridIndices);
            }
            else
            {
                _clear.Add(tileRef.GridIndices);
            }
        }

        _job.Data.Clear();
        // Wait for seed job here

        foreach (var seed in _seeds)
        {
            if (!seed.Comp.Enabled)
                continue;

            _job.Data.Add(seed);
        }

        for (var i = _job.Vis1.Count; i < _job.Data.Count; i++)
        {
            _job.Vis1.Add(new Dictionary<Vector2i, int>());
            _job.Vis2.Add(new Dictionary<Vector2i, int>());
            _job.SeedTiles.Add(new HashSet<Vector2i>());
            _job.BoundaryTiles.Add(new HashSet<Vector2i>());
        }

        _job.Grid = grid;
        _job.VisibleTiles = visibleTiles;
        _parallel.ProcessNow(_job, _job.Data.Count);
    }

    private int GetMaxDelta(Vector2i tile, Vector2i center)
    {
        var delta = tile - center;
        return Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y));
    }

    private int GetSumDelta(Vector2i tile, Vector2i center)
    {
        var delta = tile - center;
        return Math.Abs(delta.X) + Math.Abs(delta.Y);
    }

    /// <summary>
    /// Checks if any of a tile's neighbors are visible.
    /// </summary>
    private bool CheckNeighborsVis(
        Dictionary<Vector2i, int> vis,
        Vector2i index,
        int d)
    {
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                var neighbor = index + new Vector2i(x, y);
                var neighborD = vis.GetValueOrDefault(neighbor);

                if (neighborD == d)
                    return true;
            }
        }
        return false;
    }

    /// Checks whether this tile fits the definition of a "corner"
    /// </summary>
    private bool IsCorner(
        HashSet<Vector2i> tiles,
        HashSet<Vector2i> blocked,
        Dictionary<Vector2i, int> vis1,
        Vector2i index,
        Vector2i delta)
    {
        var diagonalIndex = index + delta;

        if (!tiles.TryGetValue(diagonalIndex, out var diagonal))
            return false;

        var cardinal1 = new Vector2i(index.X, diagonal.Y);
        var cardinal2 = new Vector2i(diagonal.X, index.Y);

        return vis1.GetValueOrDefault(diagonal) != 0 &&
               vis1.GetValueOrDefault(cardinal1) != 0 &&
               vis1.GetValueOrDefault(cardinal2) != 0 &&
               blocked.Contains(cardinal1) &&
               blocked.Contains(cardinal2) &&
               !blocked.Contains(diagonal);
    }

    /// <summary>
    /// Gets the relevant vision seeds for later.
    /// </summary>
    private record struct SeedJob() : IRobustJob
    {
        public StationAiVisionSystem System;

        public Entity<MapGridComponent> Grid;
        public Box2Rotated ExpandedBounds;

        public void Execute()
        {
            var localAABB = System._xforms.GetInvWorldMatrix(Grid.Owner).TransformBox(ExpandedBounds);
            System._lookup.GetLocalEntitiesIntersecting(Grid.Owner, localAABB, System._seeds);
        }
    }

    private record struct ViewJob() : IParallelRobustJob
    {
        public int BatchSize => 1;

        public IEntityManager EntManager;
        public SharedMapSystem Maps;
        public StationAiVisionSystem System;

        public Entity<MapGridComponent> Grid;
        public List<Entity<StationAiVisionComponent>> Data = new();

        public HashSet<Vector2i> VisibleTiles;

        public readonly List<Dictionary<Vector2i, int>> Vis1 = new();
        public readonly List<Dictionary<Vector2i, int>> Vis2 = new();

        public readonly List<HashSet<Vector2i>> SeedTiles = new();
        public readonly List<HashSet<Vector2i>> BoundaryTiles = new();

        public void Execute(int index)
        {
            var seed = Data[index];

            // Fastpath just get tiles in range.
            if (!seed.Comp.Occluded)
            {
                return;
            }

            // Code based upon https://github.com/OpenDreamProject/OpenDream/blob/c4a3828ccb997bf3722673620460ebb11b95ccdf/OpenDreamShared/Dream/ViewAlgorithm.cs

            var range = seed.Comp.Range;
            var vis1 = Vis1[index];
            var vis2 = Vis2[index];

            var seedTiles = SeedTiles[index];
            var boundary = BoundaryTiles[index];
            var maxDepthMax = 0;
            var sumDepthMax = 0;

            var eyePos = Maps.GetTileRef(Grid.Owner, Grid, EntManager.GetComponent<TransformComponent>(seed).Coordinates).GridIndices;

            for (var x = Math.Floor(eyePos.X - range); x <= eyePos.X + range; x++)
            {
                for (var y = Math.Floor(eyePos.Y - range); y <= eyePos.Y + range; y++)
                {
                    var tile = new Vector2i((int)x, (int)y);
                    var delta = tile - eyePos;
                    var xDelta = Math.Abs(delta.X);
                    var yDelta = Math.Abs(delta.Y);

                    var deltaSum = xDelta + yDelta;

                    maxDepthMax = Math.Max(maxDepthMax, Math.Max(xDelta, yDelta));
                    sumDepthMax = Math.Max(sumDepthMax, deltaSum);
                    seedTiles.Add(tile);
                }
            }

            // Step 3, Diagonal shadow loop
            for (var d = 0; d < maxDepthMax; d++)
            {
                foreach (var tile in seedTiles)
                {
                    var maxDelta = System.GetMaxDelta(tile, eyePos);

                    if (maxDelta == d + 1 && System.CheckNeighborsVis(vis2, tile, d))
                    {
                        vis2[tile] = (System._opaque.Contains(tile) ? -1 : d + 1);
                    }
                }
            }

            // Step 4, Straight shadow loop
            for (var d = 0; d < sumDepthMax; d++)
            {
                foreach (var tile in seedTiles)
                {
                    var sumDelta = System.GetSumDelta(tile, eyePos);

                    if (sumDelta == d + 1 && System.CheckNeighborsVis(vis1, tile, d))
                    {
                        if (System._opaque.Contains(tile))
                        {
                            vis1[tile] = -1;
                        }
                        else if (vis2.GetValueOrDefault(tile) != 0)
                        {
                            vis1[tile] = d + 1;
                        }
                    }
                }
            }

            // Add the eye itself
            vis1[eyePos] = 1;

            // Step 6.

            // Step 7.

            // Step 8.
            foreach (var tile in seedTiles)
            {
                vis2[tile] = vis1.GetValueOrDefault(tile, 0);
            }

            // Step 9
            foreach (var tile in seedTiles)
            {
                if (!System._opaque.Contains(tile))
                    continue;

                var tileVis1 = vis1.GetValueOrDefault(tile);

                if (tileVis1 != 0)
                    continue;

                if (System.IsCorner(seedTiles, System._opaque, vis1, tile, Vector2i.UpRight) ||
                    System.IsCorner(seedTiles, System._opaque, vis1, tile, Vector2i.UpLeft) ||
                    System.IsCorner(seedTiles, System._opaque, vis1, tile, Vector2i.DownLeft) ||
                    System.IsCorner(seedTiles, System._opaque, vis1, tile, Vector2i.DownRight))
                {
                    boundary.Add(tile);
                }
            }

            // Make all wall/corner tiles visible
            foreach (var tile in boundary)
            {
                vis1[tile] = -1;
            }

            // vis2 is what we care about for LOS.
            foreach (var tile in seedTiles)
            {
                // If not in viewport don't care.
                if (!System._viewportTiles.Contains(tile))
                    continue;

                var tileVis2 = vis2.GetValueOrDefault(tile, 0);

                if (tileVis2 != 0)
                {
                    // No idea if it's better to do this inside or out.
                    lock (VisibleTiles)
                    {
                        VisibleTiles.Add(tile);
                    }
                }
            }

            vis1.Clear();
            vis2.Clear();

            seedTiles.Clear();
            boundary.Clear();
        }
    }
}
