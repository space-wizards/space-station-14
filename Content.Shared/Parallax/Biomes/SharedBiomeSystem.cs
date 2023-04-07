using System.Diagnostics.CodeAnalysis;
using Content.Shared.Maps;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
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

    protected T Pick<T>(List<T> collection, float value)
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

    protected int Pick(int count, float value)
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

    public bool TryGetBiomeTile(EntityUid uid, MapGridComponent grid, FastNoiseLite noise, Vector2i indices, [NotNullWhen(true)] out Tile? tile)
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

        return TryGetBiomeTile(indices, ProtoManager.Index<BiomePrototype>(biome.BiomePrototype),
            biome.Noise, grid, out tile);
    }

    /// <summary>
    /// Tries to get the tile, real or otherwise, for the specified indices.
    /// </summary>
    public bool TryGetBiomeTile(Vector2i indices, BiomePrototype prototype, FastNoiseLite noise, MapGridComponent? grid, [NotNullWhen(true)] out Tile? tile)
    {
        if (grid?.TryGetTileRef(indices, out var tileRef) == true && !tileRef.Tile.IsEmpty)
        {
            tile = tileRef.Tile;
            return true;
        }

        var oldSeed = noise.GetSeed();

        for (var i = prototype.Layers.Count - 1; i >= 0; i--)
        {
            var layer = prototype.Layers[i];

            if (layer is not BiomeTileLayer tileLayer)
                continue;

            SetNoise(noise, oldSeed, layer.Noise);

            if (TryGetTile(indices, noise, tileLayer.Threshold, ProtoManager.Index<ContentTileDefinition>(tileLayer.Tile), tileLayer.Variants, out tile))
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
    private bool TryGetTile(Vector2i indices, FastNoiseLite seed, float threshold, ContentTileDefinition tileDef, List<byte>? variants, [NotNullWhen(true)] out Tile? tile)
    {
        var found = seed.GetNoise(indices.X, indices.Y);

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
    protected bool TryGetEntity(Vector2i indices, BiomePrototype prototype, FastNoiseLite noise, MapGridComponent grid,
        [NotNullWhen(true)] out string? entity)
    {
        if (!TryGetBiomeTile(indices, prototype, noise, grid, out var tileRef))
        {
            entity = null;
            return false;
        }

        var tileId = TileDefManager[tileRef.Value.TypeId].ID;
        var oldSeed = noise.GetSeed();

        for (var i = prototype.Layers.Count - 1; i >= 0; i--)
        {
            var layer = prototype.Layers[i];

            // Decals might block entity so need to check if there's one in front of us.
            switch (layer)
            {
                case IBiomeWorldLayer worldLayer:
                    if (!worldLayer.AllowedTiles.Contains(tileId))
                        continue;

                    break;
                default:
                    continue;
            }

            SetNoise(noise, oldSeed, layer.Noise);
            var value = noise.GetNoise(indices.X, indices.Y);

            if (value < layer.Threshold)
            {
                continue;
            }

            if (layer is not BiomeEntityLayer biomeLayer)
            {
                entity = null;
                noise.SetSeed(oldSeed);
                return false;
            }

            entity = Pick(biomeLayer.Entities, (noise.GetNoise(indices.X, indices.Y, i) + 1f) / 2f);
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
    public bool TryGetDecals(Vector2i indices, BiomePrototype prototype, FastNoiseLite noise, MapGridComponent grid,
        [NotNullWhen(true)] out List<(string ID, Vector2 Position)>? decals)
    {
        if (!TryGetBiomeTile(indices, prototype, noise, grid, out var tileRef))
        {
            decals = null;
            return false;
        }

        var tileId = TileDefManager[tileRef.Value.TypeId].ID;
        var oldSeed = noise.GetSeed();

        for (var i = prototype.Layers.Count - 1; i >= 0; i--)
        {
            var layer = prototype.Layers[i];

            // Entities might block decal so need to check if there's one in front of us.
            switch (layer)
            {
                case IBiomeWorldLayer worldLayer:
                    if (!worldLayer.AllowedTiles.Contains(tileId))
                        continue;

                    break;
                default:
                    continue;
            }

            SetNoise(noise, oldSeed, layer.Noise);

            // Check if the other layer should even render, if not then keep going.
            if (layer is not BiomeDecalLayer decalLayer)
            {
                if (noise.GetNoise(indices.X, indices.Y) < layer.Threshold)
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
