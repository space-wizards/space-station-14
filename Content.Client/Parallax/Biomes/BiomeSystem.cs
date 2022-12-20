using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceCache _resourceManager = default!;

    private const int ChunkSize = 4;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new BiomeOverlay(_protoManager, this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<BiomeOverlay>();
    }

    // For now we'll just use

    public Tile GetTile(Vector2i indices, BiomePrototype prototype, int seed)
    {
        return Tile.Empty;
    }

    public Texture GetTileTexture(Vector2i indices, BiomePrototype prototype, int seed)
    {
        // TODO: Should do this per chunk and just not render the tiles unneeded.
        var chunk = SharedMapSystem.GetChunkIndices(indices, ChunkSize);
        var chunkSeed = (chunk.X + chunk.Y * ChunkSize * 64) ;
        var random = new Random(seed + chunkSeed);
        var relative = SharedMapSystem.GetChunkRelative(indices, ChunkSize);

        if (indices == Vector2i.Zero)
        {

        }

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                if (x != relative.X || y != relative.Y)
                {
                    random.NextDouble();
                    continue;
                }

                var value = random.NextDouble();
                ResourcePath path;

                if (value < 0.5)
                {
                    path = new ResourcePath("/Textures/Tiles/Planet/grass.rsi/grass0.png");
                }
                else
                {
                    path = new ResourcePath("/Textures/Tiles/grassjungle.png");
                }

                // var resource = _protoManager.Index<ContentTileDefinition>("FloorSteel").Sprite;

                //if (resource == null)
                //    continue;

                // TODO: Use the weighted random stuff.
                return _resourceManager.GetResource<TextureResource>(path).Texture;
            }
        }

        throw new InvalidOperationException();
    }
}
