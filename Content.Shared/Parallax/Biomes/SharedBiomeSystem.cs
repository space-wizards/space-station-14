using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Console;
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

    // TODO: After I wrote all of this FastNoiseLite got ported so this needs updating for that don't @ me

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiomeComponent, ComponentGetState>(OnBiomeGetState);
        SubscribeLocalEvent<BiomeComponent, ComponentHandleState>(OnBiomeHandleState);
    }

    private void OnBiomeHandleState(EntityUid uid, BiomeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BiomeComponentState state)
            return;

        component.Seed = state.Seed;
    }

    private void OnBiomeGetState(EntityUid uid, BiomeComponent component, ref ComponentGetState args)
    {
        args.State = new BiomeComponentState(component.Seed, component.BiomePrototype);
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

    public bool TryGetBiomeTile(EntityUid uid, MapGridComponent grid, Vector2i indices, [NotNullWhen(true)] out Tile? tile)
    {
        if (grid.TryGetTileRef(indices, out var tileRef))
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
            new FastNoise(biome.Seed), grid, out tile);
    }

    /// <summary>
    /// Tries to get the tile, real or otherwise, for the specified indices.
    /// </summary>
    public bool TryGetBiomeTile(Vector2i indices, BiomePrototype prototype, FastNoise seed, MapGridComponent? grid, [NotNullWhen(true)] out Tile? tile)
    {
        if (grid?.TryGetTileRef(indices, out var tileRef) == true && !tileRef.Tile.IsEmpty)
        {
            tile = tileRef.Tile;
            return true;
        }

        var oldFrequency = seed.GetFrequency();

        for (var i = prototype.Layers.Count - 1; i >= 0; i--)
        {
            var layer = prototype.Layers[i];

            if (layer is not BiomeTileLayer tileLayer)
                continue;

            seed.SetFrequency(tileLayer.Frequency);

            if (TryGetTile(indices, seed, tileLayer.Threshold, ProtoManager.Index<ContentTileDefinition>(tileLayer.Tile), tileLayer.Variants, out tile))
            {
                seed.SetFrequency(oldFrequency);
                return true;
            }
        }

        seed.SetFrequency(oldFrequency);
        tile = null;
        return false;
    }

    /// <summary>
    /// Tries to get the relevant entity for this tile.
    /// </summary>
    protected bool TryGetEntity(Vector2i indices, BiomePrototype prototype, FastNoise noise, MapGridComponent grid,
        [NotNullWhen(true)] out string? entity)
    {
        if (!TryGetBiomeTile(indices, prototype, noise, grid, out var tileRef))
        {
            entity = null;
            return false;
        }

        var tileId = TileDefManager[tileRef.Value.TypeId].ID;
        var oldFrequency = noise.GetFrequency();
        var seed = noise.GetSeed();

        for (var i = prototype.Layers.Count - 1; i >= 0; i--)
        {
            var layer = prototype.Layers[i];
            var offset = 0;

            // Decals might block entity so need to check if there's one in front of us.
            switch (layer)
            {
                case IBiomeWorldLayer worldLayer:
                    if (!worldLayer.AllowedTiles.Contains(tileId))
                        continue;

                    offset = worldLayer.SeedOffset;
                    noise.SetSeed(seed + offset);
                    noise.SetFrequency(worldLayer.Frequency);
                    break;
                default:
                    continue;
            }

            var value = (noise.GetCellular(indices.X, indices.Y) + 1f) / 2f;

            if (value < layer.Threshold)
            {
                DebugTools.Assert(value is <= 1f and >= 0f);
                continue;
            }

            if (layer is not BiomeEntityLayer biomeLayer)
            {
                entity = null;
                noise.SetFrequency(oldFrequency);
                noise.SetSeed(seed);
                return false;
            }

            entity = Pick(biomeLayer.Entities, (noise.GetSimplex(indices.X, indices.Y) + 1f) / 2f);
            noise.SetFrequency(oldFrequency);
            noise.SetSeed(seed);
            return true;
        }

        noise.SetFrequency(oldFrequency);
        noise.SetSeed(seed);
        entity = null;
        return false;
    }

    /// <summary>
    /// Tries to get the relevant decals for this tile.
    /// </summary>
    public bool TryGetDecals(Vector2i indices, BiomePrototype prototype, FastNoise noise, MapGridComponent grid,
        [NotNullWhen(true)] out List<(string ID, Vector2 Position)>? decals)
    {
        if (!TryGetBiomeTile(indices, prototype, noise, grid, out var tileRef))
        {
            decals = null;
            return false;
        }

        var tileId = TileDefManager[tileRef.Value.TypeId].ID;
        var oldFrequency = noise.GetFrequency();
        var seed = noise.GetSeed();

        for (var i = prototype.Layers.Count - 1; i >= 0; i--)
        {
            var layer = prototype.Layers[i];
            var offset = 0;

            // Entities might block decal so need to check if there's one in front of us.
            switch (layer)
            {
                case IBiomeWorldLayer worldLayer:
                    if (!worldLayer.AllowedTiles.Contains(tileId))
                        continue;

                    offset = worldLayer.SeedOffset;
                    noise.SetSeed(seed + offset);
                    noise.SetFrequency(worldLayer.Frequency);
                    break;
                default:
                    continue;
            }

            // Check if the other layer should even render, if not then keep going.
            if (layer is not BiomeDecalLayer decalLayer)
            {
                if ((noise.GetCellular(indices.X, indices.Y) + 1f) / 2f < layer.Threshold)
                    continue;

                decals = null;
                noise.SetFrequency(oldFrequency);
                noise.SetSeed(seed);
                return false;
            }

            decals = new List<(string ID, Vector2 Position)>();

            for (var x = 0; x < decalLayer.Divisions; x++)
            {
                for (var y = 0; y < decalLayer.Divisions; y++)
                {
                    var index = new Vector2(indices.X + x * 1f / decalLayer.Divisions, indices.Y + y * 1f / decalLayer.Divisions);
                    var decalValue = (noise.GetCellular(index.X, index.Y) + 1f) / 2f;

                    if (decalValue < decalLayer.Threshold)
                        continue;

                    DebugTools.Assert(decalValue is <= 1f and >= 0f);
                    decals.Add((Pick(decalLayer.Decals, (noise.GetSimplex(index.X, index.Y) + 1f) / 2f), index));
                }
            }

            noise.SetFrequency(oldFrequency);
            noise.SetSeed(seed);

            // Check other layers
            if (decals.Count == 0)
                continue;

            return true;
        }

        noise.SetFrequency(oldFrequency);
        noise.SetSeed(seed);
        decals = null;
        return false;
    }

    /// <summary>
    /// Gets the underlying biome tile, ignoring any existing tile that may be there.
    /// </summary>
    public bool TryGetTile(Vector2i indices, FastNoise seed, float threshold, ContentTileDefinition tileDef, List<byte>? variants, [NotNullWhen(true)] out Tile? tile)
    {
        if (threshold > 0f)
        {
            var found = (seed.GetSimplexFractal(indices.X, indices.Y) + 1f) / 2f;

            if (found < threshold)
            {
                tile = null;
                return false;
            }
        }

        byte variant = 0;
        var variantCount = variants?.Count ?? tileDef.Variants;

        // Pick a variant tile if they're available as well
        if (variantCount > 1)
        {
            var variantValue = (seed.GetSimplex(indices.X * 2f, indices.Y * 2f) + 1f) / 2f;
            variant = (byte) Pick(variantCount, variantValue);

            if (variants != null)
            {
                variant = variants[variant];
            }
        }

        tile = new Tile(tileDef.TileId, 0, variant);
        return true;
    }

    [Serializable, NetSerializable]
    private sealed class BiomeComponentState : ComponentState
    {
        public int Seed;
        public string Prototype;

        public BiomeComponentState(int seed, string prototype)
        {
            Seed = seed;
            Prototype = prototype;
        }
    }
}
