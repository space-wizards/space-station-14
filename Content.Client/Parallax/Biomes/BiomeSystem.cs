using System.Diagnostics.CodeAnalysis;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IClientTileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public const int ChunkSize = 8;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new BiomeOverlay(_tileDefManager, EntityManager, _mapManager, _protoManager, _resource, this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<BiomeOverlay>();
    }

    private T Pick<T>(List<T> collection, float value)
    {
        DebugTools.Assert(value is >= 0f and <= 1f);

        if (collection.Count == 1)
            return collection[0];

        value *= collection.Count;

        foreach (var item in collection)
        {
            value -= 1f;

            if (value <= 0f)
            {
                return item;
            }
        }

        throw new ArgumentOutOfRangeException();
    }

    private int Pick(int count, float value)
    {
        DebugTools.Assert(value is >= 0f and <= 1f);

        if (count == 1)
            return 0;

        value *= count;

        for (var i = 0; i < count; i++)
        {
            value -= 1f;

            if (value <= 0f)
            {
                return i;
            }
        }

        throw new ArgumentOutOfRangeException();
    }

    /// <summary>
    /// Gets the underlying biome tile, ignoring any existing tile that may be there.
    /// </summary>
    public Tile GetTile(Vector2i indices, FastNoise seed, List<ContentTileDefinition> tiles)
    {
        var value = (seed.GetSimplex(indices.X, indices.Y) + 1f) / 2f;
        var tileDef = Pick(tiles, value);
        byte variant = 0;

        // Pick a variant tile if they're available as well
        if (tileDef.Variants > 1)
        {
            var variantValue = (seed.GetSimplex(indices.X * 2f, indices.Y * 2f) + 1f) / 2f;
            variant = (byte) Pick(tileDef.Variants, variantValue);
        }

        return new Tile(tileDef.TileId, 0, variant);
    }

    public bool TryGetDecal(
        Vector2i indices,
        FastNoise seed,
        float threshold,
        List<SpriteSpecifier> decals,
        [NotNullWhen(true)] out Texture? texture)
    {
        var value = (seed.GetCellular(indices.X, indices.Y) + 1f) / 2f;

        if (value <= threshold)
        {
            texture = null;
            return false;
        }

        var decal = Pick(decals, (value - 0.5f) * 2f);
        texture = _sprite.Frame0(decal);
        return true;
    }
}
