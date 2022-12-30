using System.Linq;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeOverlay : Overlay
{
    /*
     * Similar to ParallaxOverlay except it renders fake tiles for planetmap purposes.
     */

    private readonly IClientTileDefinitionManager _tileDefinitionManager;
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototype;
    private readonly IResourceCache _resource;
    private readonly BiomeSystem _biome;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    private Dictionary<Type, HashSet<Vector2i>> _handled = new();

    public BiomeOverlay(
        IClientTileDefinitionManager tileDefinitionManager,
        IEntityManager entManager,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        IResourceCache resource,
        BiomeSystem biome)
    {
        _tileDefinitionManager = tileDefinitionManager;
        _entManager = entManager;
        _mapManager = mapManager;
        _prototype = protoManager;
        _resource = resource;
        _biome = biome;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var worldHandle = args.WorldHandle;
        var seed = new FastNoise(0);
        seed.SetFrequency(0.1f);
        var biome = _prototype.Index<Biome>("Grasslands");
        var tileSize = 1;

        if (_entManager.TryGetComponent<MapGridComponent>(_mapManager.GetMapEntityId(args.MapId), out var grid))
        {
            tileSize = grid.TileSize;
        }

        var tileDimensions = new Vector2(tileSize, tileSize);

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft;

        // Floor to background size.
        flooredBL = (args.WorldAABB.BottomLeft / tileSize).Floored() * tileSize;
        var ceilingTR = (args.WorldAABB.TopRight / tileSize).Ceiled() * tileSize;

        // Setup for per-tile drawing

        for (var i = biome.Layers.Count - 1; i >= 0; i--)
        {
            var layer = biome.Layers[i];
            var hands = _handled.GetOrNew(layer.GetType());

            switch (layer)
            {
                case BiomeTileLayer tileLayer:
                    DrawTileLayer(worldHandle, tileDimensions, tileLayer, flooredBL, ceilingTR, grid, seed, hands);
                    break;
                case BiomeDecalLayer decalLayer:
                    DrawDecalLayer(worldHandle, tileDimensions, decalLayer, flooredBL, ceilingTR, grid, seed, hands);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        foreach (var handled in _handled.Values)
        {
            handled.Clear();
        }
    }

    private void DrawTileLayer(
        DrawingHandleWorld screenHandle,
        Vector2 tileSize,
        BiomeTileLayer tileLayer,
        Vector2 flooredBL,
        Vector2 ceilingTR,
        MapGridComponent? grid,
        FastNoise seed,
        HashSet<Vector2i> handledTiles)
    {
        seed.SetFrequency(tileLayer.Frequency);
        var groups = tileLayer.Tiles.Select(o => _prototype.Index<ContentTileDefinition>(o)).ToList();

        for (var x = flooredBL.X; x < ceilingTR.X; x++)
        {
            for (var y = flooredBL.Y; y < ceilingTR.Y; y++)
            {
                var indices = new Vector2i((int) x, (int) y);

                // If there's a tile there then skip drawing.
                if (grid?.TryGetTileRef(indices, out _) == true || !handledTiles.Add(indices))
                    continue;

                var tile = _biome.GetTile(indices, seed, groups);
                var tex = _tileDefinitionManager.GetTexture(tile);
                screenHandle.DrawTextureRect(tex, Box2.FromDimensions(indices, tileSize));
            }
        }
    }

    private void DrawDecalLayer(
        DrawingHandleWorld screenHandle,
        Vector2 tileSize,
        BiomeDecalLayer decalLayer,
        Vector2 flooredBL,
        Vector2 ceilingTR,
        MapGridComponent? grid,
        FastNoise seed,
        HashSet<Vector2i> handled)
    {
        seed.SetFrequency(decalLayer.Frequency);
        seed.SetSeed(seed.GetSeed() + decalLayer.SeedOffset);

        for (var x = flooredBL.X - 1f; x < ceilingTR.X; x++)
        {
            for (var y = flooredBL.Y - 1f; y < ceilingTR.Y; y++)
            {
                var indices = new Vector2i((int) x, (int) y);

                // If there's a tile there then skip drawing.
                if (grid?.TryGetTileRef(indices, out _) == true || handled.Contains(indices))
                    continue;

                var drawn = false;

                for (var i = 0; i < decalLayer.Divisions; i++)
                {
                    for (var j = 0; j < decalLayer.Divisions; j++)
                    {
                        var index = new Vector2(x + i * 1f / decalLayer.Divisions, y + j * 1f / decalLayer.Divisions);

                        if (!_biome.TryGetDecal(index, seed, decalLayer.Threshold, decalLayer.Decals, out var tex))
                            continue;

                        drawn = true;
                        screenHandle.DrawTextureRect(tex, Box2.FromDimensions(index, tileSize));
                    }
                }

                if (drawn)
                    handled.Add(indices);
            }
        }

        seed.SetSeed(seed.GetSeed() - decalLayer.SeedOffset);
    }
}
