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

    private readonly IPrototypeManager _prototype = default!;
    private BiomeSystem _biome = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public BiomeOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var screenHandle = args.WorldHandle;
        var seed = 0;
        var biome = _prototype.Index<BiomePrototype>("Grass");
        const float TileSize = 1f;

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft;

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
