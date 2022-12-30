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
        var biome = _prototype.Index<BiomePrototype>("Grasslands");
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

            switch (_prototype.Index<BiomeLayerPrototype>(layer))
            {
                case BiomeTileLayer tileLayer:
                    DrawTileLayer(worldHandle, tileDimensions, tileLayer, flooredBL, ceilingTR, grid, seed);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    private void DrawTileLayer(DrawingHandleWorld screenHandle, Vector2 tileSize, BiomeTileLayer tileLayer, Vector2 flooredBL, Vector2 ceilingTR, MapGridComponent? grid, FastNoise seed)
    {
        var groups = tileLayer.Tiles.Select(o => _prototype.Index<ContentTileDefinition>(o)).ToList();

        for (var x = flooredBL.X; x < ceilingTR.X; x++)
        {
            for (var y = flooredBL.Y; y < ceilingTR.Y; y++)
            {
                var indices = new Vector2i((int) x, (int) y);

                // If there's a tile there then skip drawing.
                if (grid?.TryGetTileRef(indices, out _) == true)
                    continue;

                var tile = _biome.GetTile(indices, seed, groups);
                var tex = _tileDefinitionManager.GetTexture(tile);
                screenHandle.DrawTextureRect(tex, Box2.FromDimensions(indices, tileSize));
            }
        }
    }
}
