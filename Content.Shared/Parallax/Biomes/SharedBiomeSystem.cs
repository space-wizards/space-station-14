using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

    /// <summary>
    /// Cache of tiles we've calculated previously.
    /// </summary>
    protected Dictionary<BiomePrototype, Dictionary<int, Dictionary<Vector2i, Tile>>> TileCache = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiomeComponent, ComponentGetState>(OnBiomeGetState);
        SubscribeLocalEvent<BiomeComponent, ComponentHandleState>(OnBiomeHandleState);
        SubscribeLocalEvent<BiomeComponent, ComponentShutdown>(OnBiomeShutdown);
    }

    private void OnBiomeShutdown(EntityUid uid, BiomeComponent component, ComponentShutdown args)
    {
        // Cleanup caching to avoid leaking over long-term.
        if (ProtoManager.TryIndex<BiomePrototype>(component.BiomePrototype, out var prototype))
        {
            TileCache.Remove(prototype);
        }
    }

    private void OnBiomeHandleState(EntityUid uid, BiomeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BiomeComponentState state)
            return;

        if (component.BiomePrototype != state.Prototype && ProtoManager.TryIndex<BiomePrototype>(component.BiomePrototype, out var prototype))
        {
            TileCache.Remove(prototype);
        }

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
        if (!TryComp<BiomeComponent>(uid, out var biome))
        {
            if (grid.TryGetTileRef(indices, out var tileRef))
            {
                tile = tileRef.Tile;
                return true;
            }

            tile = null;
            return false;
        }

        return TryGetBiomeTile(indices, ProtoManager.Index<BiomePrototype>(biome.BiomePrototype),
            new FastNoise(biome.Seed), grid, out tile);
    }

    public bool TryGetBiomeTile(Vector2i indices, BiomePrototype prototype, FastNoise seed, MapGridComponent? grid, [NotNullWhen(true)] out Tile? tile)
    {
        if (grid?.TryGetTileRef(indices, out var tileRef) == true && !tileRef.Tile.IsEmpty)
        {
            tile = null;
            return false;
        }

        var oldFrequency = seed.GetFrequency();
        var biomeCache = TileCache.GetOrNew(prototype);
        var seedCache = biomeCache.GetOrNew(seed.GetSeed());

        if (seedCache.TryGetValue(indices, out var cachedTile))
        {
            tile = cachedTile;
            return true;
        }

        for (var i = prototype.Layers.Count - 1; i >= 0; i--)
        {
            var layer = prototype.Layers[i];

            if (layer is not BiomeTileLayer tileLayer)
                continue;

            seed.SetFrequency(tileLayer.Frequency);

            if (TryGetTile(indices, seed, tileLayer.Threshold, tileLayer.Tiles.Select(o => ProtoManager.Index<ContentTileDefinition>(o)).ToList(), out tile))
            {
                seed.SetFrequency(oldFrequency);
                seedCache[indices] = tile.Value;
                return true;
            }
        }

        seed.SetFrequency(oldFrequency);
        tile = null;
        return false;
    }

    /// <summary>
    /// Gets the underlying biome tile, ignoring any existing tile that may be there.
    /// </summary>
    public bool TryGetTile(Vector2i indices, FastNoise seed, float threshold, List<ContentTileDefinition> tiles, [NotNullWhen(true)] out Tile? tile)
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

        var value = (seed.GetSimplex(indices.X, indices.Y) + 1f) / 2f;
        var tileDef = Pick(tiles, value);
        byte variant = 0;

        // Pick a variant tile if they're available as well
        if (tileDef.Variants > 1)
        {
            var variantValue = (seed.GetSimplex(indices.X * 2f, indices.Y * 2f) + 1f) / 2f;
            variant = (byte) Pick(tileDef.Variants, variantValue);
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
