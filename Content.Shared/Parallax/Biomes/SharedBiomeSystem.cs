using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes.Layers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Parallax.Biomes;

public abstract class SharedBiomeSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] protected readonly ITileDefinitionManager TileDefManager = default!;

    protected const byte ChunkSize = 8;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiomeComponent, AfterAutoHandleStateEvent>(OnBiomeAfterHandleState);
    }

    private void OnBiomeAfterHandleState(EntityUid uid, BiomeComponent component, ref AfterAutoHandleStateEvent args)
    {
        component.Noise.SetSeed(component.Seed);
    }

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

        return TryGetBiomeTile(indices, biome.Layers, biome.Noise, grid, out tile);
    }

    /// <summary>
    /// Tries to get the tile, real or otherwise, for the specified indices.
    /// </summary>
    public bool TryGetBiomeTile(Vector2i indices, List<IBiomeLayer> layers, FastNoiseLite noise, MapGridComponent? grid, [NotNullWhen(true)] out Tile? tile)
    {
        if (grid?.TryGetTileRef(indices, out var tileRef) == true && !tileRef.Tile.IsEmpty)
        {
            tile = tileRef.Tile;
            return true;
        }

        var oldSeed = noise.GetSeed();

        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];

            // Check if the tile is from meta layer, otherwise fall back to default layers.
            if (layer is BiomeMetaLayer meta)
            {
                SetNoise(noise, oldSeed, layer.Noise);
                var found = noise.GetNoise(indices.X, indices.Y);
                found *= layer.Invert ? -1 : 1;

                if (found > layer.Threshold && TryGetBiomeTile(indices, ProtoManager.Index<BiomeTemplatePrototype>(meta.Template).Layers, noise,
                        grid, out tile))
                {
                    noise.SetSeed(oldSeed);
                    return true;
                }

                continue;
            }

            if (layer is not BiomeTileLayer tileLayer)
                continue;

            SetNoise(noise, oldSeed, layer.Noise);

            if (TryGetTile(indices, noise, tileLayer.Invert, tileLayer.Threshold, ProtoManager.Index<ContentTileDefinition>(tileLayer.Tile), tileLayer.Variants, out tile))
            {
                noise.SetSeed(oldSeed);
                return true;
            }
        }

        noise.SetSeed(oldSeed);
        tile = null;
        return false;
    }

    /// <summary>
    /// Gets the underlying biome tile, ignoring any existing tile that may be there.
    /// </summary>
    private bool TryGetTile(Vector2i indices, FastNoiseLite seed, bool invert, float threshold, ContentTileDefinition tileDef, List<byte>? variants, [NotNullWhen(true)] out Tile? tile)
    {
        var found = seed.GetNoise(indices.X, indices.Y);
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
            var variantValue = (seed.GetNoise(indices.X * 8, indices.Y * 8, variantCount) + 1f) / 2f;
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
    protected bool TryGetEntity(Vector2i indices, List<IBiomeLayer> layers, FastNoiseLite noise, MapGridComponent grid,
        [NotNullWhen(true)] out string? entity)
    {
        if (!TryGetBiomeTile(indices, layers, noise, grid, out var tileRef))
        {
            entity = null;
            return false;
        }

        var tileId = TileDefManager[tileRef.Value.TypeId].ID;
        var oldSeed = noise.GetSeed();

        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];

            // Decals might block entity so need to check if there's one in front of us.
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

            SetNoise(noise, oldSeed, layer.Noise);
            var invert = layer.Invert;
            var value = noise.GetNoise(indices.X, indices.Y);
            value = invert ? value * -1 : value;

            if (value < layer.Threshold)
                continue;

            if (layer is BiomeMetaLayer meta)
            {
                if (TryGetEntity(indices, ProtoManager.Index<BiomeTemplatePrototype>(meta.Template).Layers, noise, grid, out entity))
                {
                    noise.SetSeed(oldSeed);
                    return true;
                }

                continue;
            }

            if (layer is not BiomeEntityLayer biomeLayer)
            {
                entity = null;
                noise.SetSeed(oldSeed);
                return false;
            }

            var noiseValue = noise.GetNoise(indices.X, indices.Y, i);
            entity = Pick(biomeLayer.Entities, (noiseValue + 1f) / 2f);
            noise.SetSeed(oldSeed);
            return true;
        }

        noise.SetSeed(oldSeed);
        entity = null;
        return false;
    }

    /// <summary>
    /// Tries to get the relevant decals for this tile.
    /// </summary>
    public bool TryGetDecals(Vector2i indices, List<IBiomeLayer> layers, FastNoiseLite noise, MapGridComponent grid,
        [NotNullWhen(true)] out List<(string ID, Vector2 Position)>? decals)
    {
        if (!TryGetBiomeTile(indices, layers, noise, grid, out var tileRef))
        {
            decals = null;
            return false;
        }

        var tileId = TileDefManager[tileRef.Value.TypeId].ID;
        var oldSeed = noise.GetSeed();

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

            SetNoise(noise, oldSeed, layer.Noise);
            var invert = layer.Invert;

            if (layer is BiomeMetaLayer meta)
            {
                var found = noise.GetNoise(indices.X, indices.Y);
                found *= layer.Invert ? -1 : 1;

                if (found > layer.Threshold && TryGetDecals(indices, ProtoManager.Index<BiomeTemplatePrototype>(meta.Template).Layers, noise, grid, out decals))
                {
                    noise.SetSeed(oldSeed);
                    return true;
                }

                continue;
            }

            // Check if the other layer should even render, if not then keep going.
            if (layer is not BiomeDecalLayer decalLayer)
            {
                var value = noise.GetNoise(indices.X, indices.Y);
                value = invert ? value * -1 : value;

                if (value < layer.Threshold)
                    continue;

                decals = null;
                noise.SetSeed(oldSeed);
                return false;
            }

            decals = new List<(string ID, Vector2 Position)>();

            for (var x = 0; x < decalLayer.Divisions; x++)
            {
                for (var y = 0; y < decalLayer.Divisions; y++)
                {
                    var index = new Vector2(indices.X + x * 1f / decalLayer.Divisions, indices.Y + y * 1f / decalLayer.Divisions);
                    var decalValue = noise.GetNoise(index.X, index.Y);
                    decalValue = invert ? decalValue * -1 : decalValue;

                    if (decalValue < decalLayer.Threshold)
                        continue;

                    decals.Add((Pick(decalLayer.Decals, (noise.GetNoise(indices.X, indices.Y, x + y * decalLayer.Divisions) + 1f) / 2f), index));
                }
            }

            // Check other layers
            if (decals.Count == 0)
                continue;

            noise.SetSeed(oldSeed);
            return true;
        }

        noise.SetSeed(oldSeed);
        decals = null;
        return false;
    }

    private void SetNoise(FastNoiseLite noise, int oldSeed, FastNoiseLite data)
    {
        // General
        noise.SetSeed(oldSeed + data.GetSeed());
        noise.SetFrequency(data.GetFrequency());
        noise.SetNoiseType(data.GetNoiseType());

        noise.GetRotationType3D();

        // Fractal
        noise.SetFractalType(data.GetFractalType());
        noise.SetFractalOctaves(data.GetFractalOctaves());
        noise.SetFractalLacunarity(data.GetFractalLacunarity());

        // Cellular
        noise.SetCellularDistanceFunction(data.GetCellularDistanceFunction());
        noise.SetCellularReturnType(data.GetCellularReturnType());
        noise.SetCellularJitter(data.GetCellularJitter());

        // Domain warps require separate noise
    }
}
