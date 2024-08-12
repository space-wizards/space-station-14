using System.Numerics;
using Content.Client.SurveillanceCamera;
using Content.Shared.NPC;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Client.StationAi;

public sealed class StationAiOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private HashSet<Entity<SurveillanceCameraVisualsComponent>> _seeds = new();

    private IRenderTexture? _staticTexture;
    public IRenderTexture? _blep;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_blep?.Texture.Size != args.Viewport.Size)
        {
            _staticTexture?.Dispose();
            _blep?.Dispose();
            _blep = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "station-ai-stencil");
            _staticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "station-ai-static");
        }

        var worldHandle = args.WorldHandle;

        var mapId = args.MapId;
        var worldAabb = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var maps = _entManager.System<SharedMapSystem>();
        var lookups = _entManager.System<EntityLookupSystem>();
        var xforms = _entManager.System<SharedTransformSystem>();
        var visibleTiles = new HashSet<Vector2i>();

        _mapManager.TryFindGridAt(mapId, worldAabb.Center, out var gridUid, out var grid);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        // Skrunkly line of sight to be generous like the byond one.
        var expandedBounds = worldAabb.Enlarged(7.5f);
        var cleared = new HashSet<Vector2i>();
        var opaque = new HashSet<Vector2i>();
        var occluders = new HashSet<Entity<OccluderComponent>>();
        var viewportTiles = new HashSet<Vector2i>();

        if (grid != null)
        {
            // Code based upon https://github.com/OpenDreamProject/OpenDream/blob/c4a3828ccb997bf3722673620460ebb11b95ccdf/OpenDreamShared/Dream/ViewAlgorithm.cs
            var tileEnumerator = maps.GetTilesEnumerator(gridUid, grid, expandedBounds, ignoreEmpty: false);

            // Get what tiles are blocked up-front.
            while (tileEnumerator.MoveNext(out var tileRef))
            {
                var tileBounds = lookups.GetLocalBounds(tileRef.GridIndices, grid.TileSize).Enlarged(-0.05f);

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

            // TODO: Changes

            // Run seeds in parallel
            // Iterate get_hear for each camera (instead of expanding) and store vis2.

            _seeds.Clear();
            lookups.GetEntitiesIntersecting(args.MapId, expandedBounds, _seeds, LookupFlags.Static);
            var vis1 = new Dictionary<Vector2i, int>();
            var vis2 = new Dictionary<Vector2i, int>();
            var seedTiles = new HashSet<Vector2i>();
            var boundary = new HashSet<Vector2i>();

            foreach (var seed in _seeds)
            {
                var range = 7.5f;
                boundary.Clear();
                seedTiles.Clear();
                vis1.Clear();
                vis2.Clear();

                var maxDepthMax = 0;
                var sumDepthMax = 0;

                var eyePos = maps.GetTileRef(gridUid, grid, _entManager.GetComponent<TransformComponent>(seed).Coordinates).GridIndices;

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
                        var maxDelta = GetMaxDelta(tile, eyePos);

                        if (maxDelta == d + 1 && CheckNeighborsVis(vis2, tile, d))
                        {
                            vis2[tile] = (opaque.Contains(tile) ? -1 : d + 1);
                        }
                    }
                }

                // Step 4, Straight shadow loop
                for (var d = 0; d < sumDepthMax; d++)
                {
                    foreach (var tile in seedTiles)
                    {
                        var sumDelta = GetSumDelta(tile, eyePos);

                        if (sumDelta == d + 1 && CheckNeighborsVis(vis1, tile, d))
                        {
                            if (opaque.Contains(tile))
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
                    if (!opaque.Contains(tile))
                        continue;

                    var tileVis1 = vis1.GetValueOrDefault(tile);

                    if (tileVis1 != 0)
                        continue;

                    if (IsCorner(seedTiles, opaque, vis1, tile, Vector2i.One) ||
                        IsCorner(seedTiles, opaque, vis1, tile, new Vector2i(1, -1)) ||
                        IsCorner(seedTiles, opaque, vis1, tile, new Vector2i(-1, -1)) ||
                        IsCorner(seedTiles, opaque, vis1, tile, new Vector2i(-1, 1)))
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
                    var tileVis2 = vis2.GetValueOrDefault(tile, 0);

                    if (tileVis2 != 0)
                        visibleTiles.Add(tile);
                }
            }
        }

        // TODO: Combine tiles into viewer draw-calls

        // Draw visible tiles to stencil
        worldHandle.RenderInRenderTarget(_blep!, () =>
        {
            if (!gridUid.IsValid())
                return;

            var matrix = xforms.GetWorldMatrix(gridUid);
            var matty =  Matrix3x2.Multiply(matrix, invMatrix);
            worldHandle.SetTransform(matty);

            foreach (var tile in visibleTiles)
            {
                var aabb = lookups.GetLocalBounds(tile, grid!.TileSize);
                worldHandle.DrawRect(aabb, Color.White);
            }
        },
        Color.Transparent);

        // Create static texture
        worldHandle.RenderInRenderTarget(_staticTexture!,
        () =>
        {
            worldHandle.SetTransform(invMatrix);
            worldHandle.UseShader(_proto.Index<ShaderPrototype>("CameraStatic").Instance());
            worldHandle.DrawRect(worldAabb, Color.White);
        }, Color.Transparent);

        // Use the lighting as a mask
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_blep!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilDraw").Instance());
        worldHandle.DrawTextureRect(_staticTexture!.Texture, worldBounds);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);

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

    /// <summary>
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
}
