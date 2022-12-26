using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Client.Graphics;
using Robust.Client.Map;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Parallax.Biomes;

public sealed class BiomeSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly IClientTileDefinitionManager _tileDefManager = default!;

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

    public Tile GetTile(MapGridComponent component, Vector2i indices, FastNoise seed, List<BiomeTileGroupPrototype> groups, float weightSum)
    {
        if (component.TryGetTileRef(indices, out var tileRef))
            return tileRef.Tile;

        return GetTile(indices, seed, groups, weightSum);
    }

    public float GetValue(Vector2i indices, FastNoise seed)
    {
        var chunkOrigin = SharedMapSystem.GetChunkIndices(indices, ChunkSize);
        var chunkValue = (seed.GetSimplex(chunkOrigin.X * 10f, chunkOrigin.Y * 10f) + 1f) / 2f;
        var tileValue = (seed.GetSimplex(indices.X * 10f, indices.Y * 10f) + 1f) / 2f;
        return (chunkValue / 4f + tileValue) / 1.25f;
    }

    public BiomeTileGroupPrototype GetGroup(List<BiomeTileGroupPrototype> groups, float value, float weight)
    {
        DebugTools.Assert(groups.Count > 0);

        if (groups.Count == 1)
            return groups[0];

        value *= weight;

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

    /// <summary>
    /// If it's a single tile on its own we fall back to the default group
    /// </summary>
    public BiomeTileGroupPrototype GetAdjustedGroup(
        BiomeTileGroupPrototype group,
        List<BiomeTileGroupPrototype> groups,
        Vector2i indices,
        FastNoise seed,
        float weightSum)
    {
        // TODO: This API fucking blows.

        // If it's a single tile on its own we fall back to the default group
        if (group != groups[0])
        {
            var isValid = false;

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0 ||
                        x != 0 && y != 0)
                    {
                        continue;
                    }

                    var neighborIndices = new Vector2i(indices.X + x, indices.Y + y);
                    var neighborValue = GetValue(neighborIndices, seed);
                    var neighborGroup = GetGroup(groups, neighborValue, weightSum);

                    if (neighborGroup == group)
                    {
                        isValid = true;
                        break;
                    }
                }

                if (isValid)
                    break;
            }

            if (!isValid)
            {
                group = groups[0];
            }
        }

        return group;
    }

    /// <summary>
    /// Gets the underlying biome tile, ignoring any existing tile that may be there.
    /// </summary>
    public Tile GetTile(Vector2i indices, FastNoise seed, List<BiomeTileGroupPrototype> groups,
        float weightSum)
    {
        var value = GetValue(indices, seed);
        var group = GetGroup(groups, value, weightSum);
        group = GetAdjustedGroup(group, groups, indices, seed, weightSum);

        byte variant = 0;
        var tileDef = _protoManager.Index<ContentTileDefinition>(group.Tile);

        // Pick a variant tile if they're available as well
        if (tileDef.Variants > 1)
        {
            var variantValue = (seed.GetSimplex(indices.X * 20f, indices.Y * 20f) + 1f) / 2f;
            variantValue *= tileDef.Variants;

            for (byte i = 0; i < tileDef.Variants; i++)
            {
                variantValue -= 1f;

                if (variantValue <= 0f)
                {
                    variant = i;
                    break;
                }
            }
        }

        return new Tile(tileDef.TileId, 0, variant);
    }
}
