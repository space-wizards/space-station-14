using System.Linq;
using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeOverlay : Overlay
{
    /*
     * Similar to ParallaxOverlay except it renders fake tiles for planetmap purposes.
     */

    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototype;
    private readonly BiomeSystem _biome;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public BiomeOverlay(IEntityManager entManager, IMapManager mapManager, IPrototypeManager protoManager, BiomeSystem biome)
    {
        _entManager = entManager;
        _mapManager = mapManager;
        _prototype = protoManager;
        _biome = biome;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var screenHandle = args.WorldHandle;
        var seed = 0;
        var biome = _prototype.Index<BiomePrototype>("Grasslands");

        var tileSize = 1f;

        if (_entManager.TryGetComponent<MapGridComponent>(_mapManager.GetMapEntityId(args.MapId), out var grid))
        {
            tileSize = grid.TileSize;
        }

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft;

        // Floor to background size.
        flooredBL = (args.WorldAABB.BottomLeft / tileSize).Floored() * tileSize;
        var ceilingTR = (args.WorldAABB.TopRight / tileSize).Ceiled() * tileSize;

        // Setup for per-tile drawing
        var groups = biome.TileGroups.Select(o => _prototype.Index<BiomeTileGroupPrototype>(o)).ToList();
        var weightSum = groups.Sum(o => o.Weight);

        for (var x = flooredBL.X; x < ceilingTR.X; x += tileSize)
        {
            for (var y = flooredBL.Y; y < ceilingTR.Y; y+= tileSize)
            {
                var indices = new Vector2i((int) x, (int) y);

                // If there's a tile there then skip drawing.
                if (grid?.TryGetTileRef(indices, out _) == true)
                    continue;

                var tex = _biome.GetTexture(indices, seed, groups, weightSum);

                if (tex == null)
                    continue;

                screenHandle.DrawTextureRect(tex, Box2.FromDimensions(indices, (tileSize, tileSize)));
            }
        }
    }
}
