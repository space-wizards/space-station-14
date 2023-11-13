using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes.Layers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Shared.Parallax.Biomes;

public abstract class SharedBiomeSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] protected readonly ITileDefinitionManager TileDefManager = default!;

    protected const byte ChunkSize = 8;

    private T Pick<T>(List<T> collection, float value)
    {
        // Listen I don't need this exact and I'm too lazy to finetune just for random ent picking.
        value %= 1f;
        value = Math.Clamp(value, 0f, 1f);

        if (collection.Count == 1)
            return collection[0];

        var randValue = value * collection.Count;

        foreach (var item in collection)
        {
            randValue -= 1f;

            if (randValue <= 0f)
            {
                return item;
            }
        }

        throw new ArgumentOutOfRangeException();
    }

    private int Pick(int count, float value)
    {
        value %= 1f;
        value = Math.Clamp(value, 0f, 1f);

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

    public bool TryGetBiomeTile(EntityUid uid, MapGridComponent grid, Vector2i indices, [NotNullWhen(true)] out Tile? tile)
    {
        if (grid.TryGetTileRef(indices, out var tileRef) && !tileRef.Tile.IsEmpty)
        {
            tile = tileRef.Tile;
            return true;
        }

        if (!TryComp<BiomeComponent>(uid, out var biome))
        {
            tile = null;
            return false;
        }

        return TryGetBiomeTile(indices, biome.Layers, biome.Seed, grid, out tile);
    }

    /// <summary>
    /// Tries to get the tile, real or otherwise, for the specified indices.
    /// </summary>
    public bool TryGetBiomeTile(Vector2i indices, List<IBiomeLayer> layers, int seed, MapGridComponent? grid, [NotNullWhen(true)] out Tile? tile)
    {
        if (grid?.TryGetTileRef(indices, out var tileRef) == true && !tileRef.Tile.IsEmpty)
        {
            tile = tileRef.Tile;
            return true;
        }

        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];
            var noiseCopy = GetNoise(layer.Noise, seed);

            var invert = layer.Invert;
            var value = noiseCopy.GetNoise(indices.X, indices.Y);
            value = invert ? value * -1 : value;

            if (value < layer.Threshold)
                continue;

            // Check if the tile is from meta layer, otherwise fall back to default layers.
            if (layer is BiomeMetaLayer meta)
            {
                if (TryGetBiomeTile(indices, ProtoManager.Index<BiomeTemplatePrototype>(meta.Template).Layers, seed, grid, out tile))
                {
                    return true;
                }

                continue;
            }

            if (layer is not BiomeTileLayer tileLayer)
                continue;

            if (TryGetTile(indices, noiseCopy, tileLayer.Invert, tileLayer.Threshold, ProtoManager.Index<ContentTileDefinition>(tileLayer.Tile), tileLayer.Variants, out tile))
            {
                return true;
            }
        }

        tile = null;
        return false;
    }

    /// <summary>
    /// Gets the underlying biome tile, ignoring any existing tile that may be there.
    /// </summary>
    private bool TryGetTile(Vector2i indices, FastNoiseLite noise, bool invert, float threshold, ContentTileDefinition tileDef, List<byte>? variants, [NotNullWhen(true)] out Tile? tile)
    {
        var found = noise.GetNoise(indices.X, indices.Y);
        found = invert ? found * -1 : found;

        if (found < threshold)
        {
            tile = null;
            return false;
        }

        byte variant = 0;
        var variantCount = variants?.Count ?? tileDef.Variants;

        // Pick a variant tile if they're available as well
        if (variantCount > 1)
        {
            var variantValue = (noise.GetNoise(indices.X * 8, indices.Y * 8, variantCount) + 1f) / 2f;
            variant = (byte) Pick(variantCount, variantValue);

            if (variants != null)
            {
                variant = variants[variant];
            }
        }

        tile = new Tile(tileDef.TileId, 0, variant);
        return true;
    }

    /// <summary>
    /// Tries to get the relevant entity for this tile.
    /// </summary>
    public bool TryGetEntity(Vector2i indices, BiomeComponent component, MapGridComponent grid,
        [NotNullWhen(true)] out string? entity)
    {
        if (!TryGetBiomeTile(indices, component.Layers, component.Seed, grid, out var tile))
        {
            entity = null;
            return false;
        }

        return TryGetEntity(indices, component.Layers, tile.Value, component.Seed, grid, out entity);
    }


    private bool TryGetEntity(Vector2i indices, List<IBiomeLayer> layers, Tile tileRef, int seed, MapGridComponent grid,
        [NotNullWhen(true)] out string? entity)
    {
        var tileId = TileDefManager[tileRef.TypeId].ID;

        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];

            switch (layer)
            {
                case BiomeDummyLayer:
                    continue;
                case IBiomeWorldLayer worldLayer:
                    if (!worldLayer.AllowedTiles.Contains(tileId))
                        continue;

                    break;
                case BiomeMetaLayer:
                    break;
                default:
                    continue;
            }

            var noiseCopy = GetNoise(layer.Noise, seed);

            var invert = layer.Invert;
            var value = noiseCopy.GetNoise(indices.X, indices.Y);
            value = invert ? value * -1 : value;

            if (value < layer.Threshold)
                continue;

            if (layer is BiomeMetaLayer meta)
            {
                if (TryGetEntity(indices, ProtoManager.Index<BiomeTemplatePrototype>(meta.Template).Layers, tileRef, seed, grid, out entity))
                {
                    return true;
                }

                continue;
            }

            // Decals might block entity so need to check if there's one in front of us.
            if (layer is not BiomeEntityLayer biomeLayer)
            {
                entity = null;
                return false;
            }

            var noiseValue = noiseCopy.GetNoise(indices.X, indices.Y, i);
            entity = Pick(biomeLayer.Entities, (noiseValue + 1f) / 2f);
            return true;
        }

        entity = null;
        return false;
    }

    /// <summary>
    /// Tries to get the relevant decals for this tile.
    /// </summary>
    public bool TryGetDecals(Vector2i indices, List<IBiomeLayer> layers, int seed, MapGridComponent grid,
        [NotNullWhen(true)] out List<(string ID, Vector2 Position)>? decals)
    {
        if (!TryGetBiomeTile(indices, layers, seed, grid, out var tileRef))
        {
            decals = null;
            return false;
        }

        var tileId = TileDefManager[tileRef.Value.TypeId].ID;

        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];

            // Entities might block decal so need to check if there's one in front of us.
            switch (layer)
            {
                case BiomeDummyLayer:
                    continue;
                case IBiomeWorldLayer worldLayer:
                    if (!worldLayer.AllowedTiles.Contains(tileId))
                        continue;

                    break;
                case BiomeMetaLayer:
                    break;
                default:
                    continue;
            }

            var invert = layer.Invert;
            var noiseCopy = GetNoise(layer.Noise, seed);
            var value = noiseCopy.GetNoise(indices.X, indices.Y);
            value = invert ? value * -1 : value;

            if (value < layer.Threshold)
                continue;

            if (layer is BiomeMetaLayer meta)
            {
                if (TryGetDecals(indices, ProtoManager.Index<BiomeTemplatePrototype>(meta.Template).Layers, seed, grid, out decals))
                {
                    return true;
                }

                continue;
            }

            // Check if the other layer should even render, if not then keep going.
            if (layer is not BiomeDecalLayer decalLayer)
            {
                decals = null;
                return false;
            }

            decals = new List<(string ID, Vector2 Position)>();

            for (var x = 0; x < decalLayer.Divisions; x++)
            {
                for (var y = 0; y < decalLayer.Divisions; y++)
                {
                    var index = new Vector2(indices.X + x * 1f / decalLayer.Divisions, indices.Y + y * 1f / decalLayer.Divisions);
                    var decalValue = noiseCopy.GetNoise(index.X, index.Y);
                    decalValue = invert ? decalValue * -1 : decalValue;

                    if (decalValue < decalLayer.Threshold)
                        continue;

                    decals.Add((Pick(decalLayer.Decals, (noiseCopy.GetNoise(indices.X, indices.Y, x + y * decalLayer.Divisions) + 1f) / 2f), index));
                }
            }

            // Check other layers
            if (decals.Count == 0)
                continue;

            return true;
        }

        decals = null;
        return false;
    }

    private FastNoiseLite GetNoise(FastNoiseLite seedNoise, int seed)
    {
        var noiseCopy = new FastNoiseLite();
        _serManager.CopyTo(seedNoise, ref noiseCopy, notNullableOverride: true);
        noiseCopy.SetSeed(noiseCopy.GetSeed() + seed);
        // Ensure re-calculate is run.
        noiseCopy.SetFractalOctaves(noiseCopy.GetFractalOctaves());
        return noiseCopy;
    }
}
