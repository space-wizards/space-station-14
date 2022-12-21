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

    private Texture[] _textures = new Texture[BiomeSystem.ChunkSize * BiomeSystem.ChunkSize];

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
        var chunkSize = BiomeSystem.ChunkSize;
        // TODO: Map mapgrid
        var tileSize = 1f;

        // Remove offset so we can floor.
        var flooredBL = args.WorldAABB.BottomLeft;

        // Floor to background size.
        flooredBL = (args.WorldAABB.BottomLeft / chunkSize).Floored() * chunkSize;
        var ceilingTR = (args.WorldAABB.TopRight / chunkSize).Ceiled() * chunkSize;

        for (var x = flooredBL.X; x < ceilingTR.X; x += chunkSize)
        {
            for (var y = flooredBL.Y; y < ceilingTR.Y; y+= chunkSize)
            {
                var originIndices = new Vector2i((int) x, (int) y);
                _biome.GetChunkTextures(originIndices, biome, seed, ref _textures);
                // TODO: Avoid overdraw on the tile if there's an existing tile there.
                var idx = 0;

                foreach (var tex in _textures)
                {
                    var (texX, texY) = (idx / chunkSize, idx % chunkSize);
                    screenHandle.DrawTextureRect(tex, Box2.FromDimensions(originIndices + (texX, texY), (tileSize, tileSize)));
                    idx++;
                }
            }
        }
    }
}
