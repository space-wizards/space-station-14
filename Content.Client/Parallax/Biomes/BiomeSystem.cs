using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceCache _resourceManager = default!;

    public const int ChunkSize = 4;

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

    public void GetChunkTextures(Vector2i indices, BiomePrototype prototype, int seed, ref Texture[] textures)
    {
        var chunkIndices = SharedMapSystem.GetChunkIndices(indices, ChunkSize);
        DebugTools.Assert(SharedMapSystem.GetChunkRelative(indices, ChunkSize) == Vector2i.Zero);

        unchecked
        {
            var chunkSeed = (chunkIndices.X + chunkIndices.Y * ChunkSize * 64);
            seed += chunkSeed;
        }

        var random = new Random(seed);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
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

                // TODO: Should be by group
                // Inside each group is random
                // Outside groups should be perlin noise or smth?
                // Also do the edge overlaps for grass <> other groups too

                textures[x + y * ChunkSize] = _resourceManager.GetResource<TextureResource>(path).Texture;
            }
        }
    }
}
