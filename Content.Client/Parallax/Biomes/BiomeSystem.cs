using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceCache _resourceManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const int ChunkSize = 4;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new BiomeOverlay());
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
        var random = new Random(seed);
        var chunk = SharedMapSystem.GetChunkIndices(indices, ChunkSize);
        var relative = SharedMapSystem.GetChunkRelative(indices, ChunkSize);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                if (x != relative.X && y != relative.Y)
                {
                    random.Next();
                    continue;
                }

                var resource = _protoManager.Index<ContentTileDefinition>("Grass").Sprite;

                if (resource == null)
                    continue;

                // TODO: Use the weighted random stuff.
                return _resourceManager.GetResource<TextureResource>(resource).Texture;
            }
        }

        throw new InvalidOperationException();
    }
}
