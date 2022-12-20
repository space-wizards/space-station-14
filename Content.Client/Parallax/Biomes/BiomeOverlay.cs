using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeOverlay : Overlay
{
    /*
     * Similar to ParallaxOverlay except it renders fake tiles for planetmap purposes.
     */

    private readonly IPrototypeManager _prototype;
    private readonly BiomeSystem _biome;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public BiomeOverlay(IPrototypeManager protoManager, BiomeSystem biome)
    {
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
        var TileSize = 1f;

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft;
        // args.WorldHandle.SetTransform(Matrix3.Identity);

        // Floor to background size.
        flooredBL = (flooredBL / TileSize).Floored() * TileSize;

        for (var x = flooredBL.X; x < args.WorldAABB.Right; x += TileSize)
        {
            for (var y = flooredBL.Y; y < args.WorldAABB.Top; y += TileSize)
            {
                var indices = new Vector2i((int) x, (int) y);
                var tex = _biome.GetTileTexture(indices, biome, seed);

                screenHandle.DrawTextureRect(tex, Box2.FromDimensions((x, y), (TileSize, TileSize)));
            }
        }
    }
}
