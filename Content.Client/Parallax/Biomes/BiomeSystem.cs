using System.Linq;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;

    public const int ChunkSize = 8;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new BiomeOverlay(EntityManager, _mapManager, _protoManager, this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<BiomeOverlay>();
    }

    public Tile GetTile(Vector2i indices, BiomePrototype prototype, int seed)
    {
        return Tile.Empty;
    }

    private float GetValue(Vector2i indices, int seed)
    {
        var chunkOrigin = SharedMapSystem.GetChunkIndices(indices, ChunkSize);
        var chunkValue = (OpenSimplex2.Noise2(seed, chunkOrigin.X, chunkOrigin.Y) + 1f) / 2f;
        var value = (OpenSimplex2.Noise2(seed, indices.X, indices.Y) + 1f) / 2f;
        return (chunkValue / 2f + value) / 1.5f;
    }

    private BiomeTileGroupPrototype GetGroup(List<BiomeTileGroupPrototype> groups, float value)
    {
        DebugTools.Assert(groups.Count > 0);

        if (groups.Count == 1)
            return groups[0];

        var sum = groups.Sum(o => o.Weight);
        value *= sum;

        foreach (var group in groups)
        {
            value -= group.Weight;

            if (value <= 0f)
            {
                return group;
            }
        }

        throw new InvalidOperationException();
    }

    public Texture? GetTexture(Vector2i indices, BiomePrototype prototype, int seed)
    {
        var value = GetValue(indices, seed);
        var groups = prototype.TileGroups.Select(o => _protoManager.Index<BiomeTileGroupPrototype>(o)).ToList();
        var group = GetGroup(groups, value);

        var sprite = _protoManager.Index<ContentTileDefinition>(group.Tile).Sprite;

        if (sprite == null)
            return null;

        var variant = 0;

        var cache = _variantCache.GetOrNew(sprite);

        if (cache.TryGetValue(variant, out var texture))
        {
            return texture;
        }

        // TODO: TileDefManager should just have shit for this.
        using var stream = _resourceCache.ContentFileRead(sprite);
        var image = Image.Load<Rgba32>(stream);
        image = image.Clone(o => o.Crop(new Rectangle(32 * variant, 0, 32, 32)));

        cache[variant] = Texture.LoadFromImage(image);
        return cache[variant];
    }

    private Dictionary<ResourcePath, Dictionary<int, Texture>> _variantCache = new();
}
