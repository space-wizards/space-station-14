using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Shared.CCVar;
using Content.Shared.Decals;
using Content.Shared.Ghost;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Components;
using Robust.Server.Player;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class NewBiomeSystem : EntitySystem
{
    /*
     * Handles loading in biomes around players.
     * These are essentially chunked-areas that load in dungeons and can also be unloaded.
     */

    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    /// <summary>
    /// Jobs for biomes to load.
    /// </summary>
    private JobQueue _biomeQueue = default!;

    private float _checkUnloadAccumulator;
    private float _checkUnloadTime;
    private float _loadRange = 1f;
    private float _loadTime;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<NewBiomeComponent> _biomeQuery;

    public override void Initialize()
    {
        base.Initialize();
        _ghostQuery = GetEntityQuery<GhostComponent>();
        _biomeQuery = GetEntityQuery<NewBiomeComponent>();

        Subs.CVar(_cfgManager, CCVars.BiomeLoadRange, OnLoadRange, true);
        Subs.CVar(_cfgManager, CCVars.BiomeLoadTime, OnLoadTime, true);
        Subs.CVar(_cfgManager, CCVars.BiomeCheckUnloadTime, OnCheckUnload, true);
    }

    private void OnCheckUnload(float obj)
    {
        _checkUnloadTime = obj;
    }

    private void OnLoadTime(float obj)
    {
        _biomeQueue = new JobQueue(obj);
        _loadTime = obj;
    }

    private void OnLoadRange(float obj)
    {
        _loadRange = obj;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<NewBiomeComponent>();

        while (query.MoveNext(out var biome))
        {
            // If it's still loading then don't touch the observer bounds.
            if (biome.Loading)
                continue;

            biome.LoadedBounds.Clear();
        }

        // Get all relevant players.
        foreach (var player in _player.Sessions)
        {
            if (player.AttachedEntity != null)
            {
                TryAddBiomeBounds(player.AttachedEntity.Value);
            }

            foreach (var viewer in player.ViewSubscriptions)
            {
                TryAddBiomeBounds(viewer);
            }
        }

        // Unload first in case we can't catch up.
        _checkUnloadAccumulator += frameTime;

        if (_checkUnloadAccumulator > _checkUnloadTime)
        {
            // Work out chunks not in range and unload.
            UnloadChunks();

            _checkUnloadAccumulator -= _checkUnloadTime;
        }

        var loadQuery = AllEntityQuery<NewBiomeComponent, MapGridComponent>();

        // Check if any biomes are intersected and queue up loads.
        while (loadQuery.MoveNext(out var uid, out var biome, out var grid))
        {
            if (biome.Loading || biome.LoadedBounds.Count == 0)
                continue;

            biome.Loading = true;
            var job = new BiomeLoadJob(_loadTime)
            {
                Grid = (uid, grid),
                Biome = biome,
            };
            _biomeQueue.EnqueueJob(job);
        }

        // Process jobs.
        _biomeQueue.Process();
    }

    private void UnloadChunks()
    {
        var query = AllEntityQuery<NewBiomeComponent, MapGridComponent>();

        while (query.MoveNext(out var uid, out var biome, out var grid))
        {
            // Only start unloading if it's currently not loading anything.
            if (biome.Loading)
                continue;

            var toUnload = new Dictionary<string, List<Vector2i>>();

            foreach (var (layerId, loadedLayer) in biome.LoadedData)
            {
                var layer = biome.Layers[layerId];

                // If it can't unload then ignore it.
                if (!layer.CanUnload)
                    continue;

                // Go through each loaded chunk and check if they can be unloaded by checking if any players are in range.
                foreach (var chunk in loadedLayer.Keys)
                {
                    var chunkBounds = new Box2i(chunk, chunk + layer.Size);
                    var canUnload = true;

                    foreach (var playerView in biome.LoadedBounds)
                    {
                        // Still relevant
                        if (chunkBounds.Intersects(playerView))
                        {
                            canUnload = false;
                            break;
                        }
                    }

                    if (!canUnload)
                        continue;

                    toUnload.GetOrNew(layerId).Add(chunk);
                }
            }

            if (toUnload.Count == 0)
                continue;

            // Queue up unloads.
            biome.Loading = true;
            var job = new BiomeUnloadJob(_loadTime)
            {
                Biome = (uid, grid, biome),
                ToUnload = toUnload,
            };
            _biomeQueue.EnqueueJob(job);
        }
    }

    /// <summary>
    /// Gets the full bounds to be loaded. Considers layer dependencies where they may have different chunk sizes.
    /// </summary>
    private Box2i GetFullBounds(NewBiomeComponent component, Box2i bounds)
    {
        var result = bounds;

        foreach (var layer in component.Layers.Values)
        {
            var layerBounds = GetLayerBounds(layer, result);

            if (layer.DependsOn != null)
            {
                foreach (var sub in layer.DependsOn)
                {
                    var depLayer = component.Layers[sub];

                    layerBounds = layerBounds.Union(GetLayerBounds(depLayer, layerBounds));
                }
            }

            result = result.Union(layerBounds);
        }

        return result;
    }

    /// <summary>
    /// Tries to add the viewer bounds of this entity for loading.
    /// </summary>
    private void TryAddBiomeBounds(EntityUid uid)
    {
        var xform = Transform(uid);

        // No biome to load
        if (!_biomeQuery.TryComp(xform.MapUid, out var biome))
            return;

        // Currently already loading.
        if (biome.Loading)
            return;

        var center = _xforms.GetWorldPosition(uid);

        var bounds = new Box2i((center - new Vector2(_loadRange, _loadRange)).Floored(), (center + new Vector2(_loadRange, _loadRange)).Floored());

        var adjustedBounds = GetFullBounds(biome, bounds);
        biome.LoadedBounds.Add(adjustedBounds);
    }

    public Box2i GetLayerBounds(NewBiomeMetaLayer layer, Box2i layerBounds)
    {
        var chunkSize = new Vector2(layer.Size, layer.Size);

        // Need to round the bounds to our chunk size to ensure we load whole chunks.
        // We also need to know the minimum bounds for our dependencies to load.
        var layerBL = (layerBounds.BottomLeft / chunkSize).Floored() * chunkSize;
        var layerTR = (layerBounds.TopRight / chunkSize).Ceiled() * chunkSize;

        var loadBounds = new Box2i(layerBL.Floored(), layerTR.Ceiled());
        return loadBounds;
    }
}

 public sealed class BiomeLoadJob : Job<bool>
 {
     [Dependency] private IEntityManager _entManager = default!;
     [Dependency] private IPrototypeManager _protoManager = default!;
     private NewBiomeSystem System = default!;

    public Entity<MapGridComponent> Grid;

    /// <summary>
    /// Biome that is getting loaded.
    /// </summary>
    public NewBiomeComponent Biome = default!;

    public BiomeLoadJob(double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        IoCManager.InjectDependencies(this);
        System = _entManager.System<NewBiomeSystem>();
    }

    protected override async Task<bool> Process()
    {
        try
        {
            foreach (var bound in Biome.LoadedBounds)
            {
                foreach (var (layerId, layer) in Biome.Layers)
                {
                    await LoadLayer(layerId, layer, bound);
                }
            }
        }
        finally
        {
            // Finished
            DebugTools.Assert(Biome.Loading);
            Biome.Loading = false;
        }

        return true;
    }

    private async Task LoadLayer(string layerId, NewBiomeMetaLayer layer, Box2i parentBounds)
    {
        var loadBounds = System.GetLayerBounds(layer, parentBounds);

        // Make sure our dependencies are loaded first.
        if (layer.DependsOn != null)
        {
            foreach (var sub in layer.DependsOn)
            {
                var actualLayer = Biome.Layers[sub];

                await LoadLayer(sub, actualLayer, loadBounds);
            }
        }

        // Okay all of our dependencies loaded so we can send it.
        var chunkEnumerator = new ChunkIndicesEnumerator(loadBounds, layer.Size);

        while (chunkEnumerator.MoveNext(out var chunk))
        {
            var chunkOrigin = chunk.Value * layer.Size;
            var layerLoaded = Biome.LoadedData.GetOrNew(layerId);

            // Layer already loaded for this chunk.
            // This can potentially happen if we're moving and the player's bounds changed but some existing chunks remain.
            if (layerLoaded.ContainsKey(chunkOrigin))
            {
                continue;
            }

            // Load dungeon here async await and all that jaz.
            var (_, data) = await WaitAsyncTask(_entManager
                .System<DungeonSystem>()
                .GenerateDungeonAsync(_protoManager.Index(layer.Dungeon), Grid.Owner, Grid.Comp, chunkOrigin, Biome.Seed, reservedTiles: Biome.ModifiedTiles));

            // If we can unload it then store the data to check for later.
            if (layer.CanUnload)
            {
                layerLoaded.Add(chunkOrigin, data);
            }
        }
    }
}

public sealed class BiomeUnloadJob : Job<bool>
{
    [Dependency] private EntityManager _entManager = default!;

    public Entity<MapGridComponent, NewBiomeComponent> Biome;
    public Dictionary<string, List<Vector2i>> ToUnload = default!;

    private static readonly List<string> _ignoredComponents = new()
    {
        "RandomSprite",
    };

    public BiomeUnloadJob(double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        IoCManager.InjectDependencies(this);
    }

    public BiomeUnloadJob(double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
    {
    }

    protected override async Task<bool> Process()
    {
        try
        {
            var grid = Biome.Comp1;
            var biome = Biome.Comp2;
            DebugTools.Assert(biome.Loading);
            var maps = _entManager.System<SharedMapSystem>();
            var decals = _entManager.System<DecalSystem>();
            var lookup = _entManager.System<EntityLookupSystem>();
            _entManager.TryGetComponent(Biome.Owner, out DecalGridComponent? decalGrid);
            var forceUnload = _entManager.GetEntityQuery<BiomeForceUnloadComponent>();
            var entities = new HashSet<EntityUid>();
            var tiles = new List<(Vector2i, Tile)>();

            foreach (var (layer, chunkOrigins) in ToUnload)
            {
                if (!biome.Layers.TryGetValue(layer, out var meta))
                    continue;

                if (!biome.LoadedData.TryGetValue(layer, out var data))
                    continue;

                DebugTools.Assert(meta.CanUnload);

                foreach (var chunk in chunkOrigins)
                {
                    // Not loaded anymore?
                    if (!data.Remove(chunk, out var loaded))
                        continue;

                    tiles.Clear();

                    foreach (var (ent, pos) in loaded.Entities)
                    {
                        // IsDefault is actually super expensive so really need to run this check in the loop.
                        await SuspendIfOutOfTime();

                        if (forceUnload.HasComp(ent))
                        {
                            _entManager.DeleteEntity(ent);
                            continue;
                        }

                        // Deleted so counts as modified.
                        if (!_entManager.TransformQuery.TryComp(ent, out var xform))
                        {
                            biome.ModifiedTiles.Add(pos);
                            continue;
                        }

                        // If it stayed still and had no data change then keep it.
                        if (pos == xform.LocalPosition.Floored() && xform.GridUid == Biome.Owner && _entManager.IsDefault(ent, _ignoredComponents))
                        {
                            _entManager.DeleteEntity(ent);
                            continue;
                        }

                        // Need the entity's current tile to be flagged for unloading.
                        if (Biome.Owner == xform.GridUid)
                        {
                            var entTile = maps.LocalToTile(Biome.Owner, grid, xform.Coordinates);
                            biome.ModifiedTiles.Add(entTile);
                        }
                    }

                    foreach (var (decal, pos) in loaded.Decals)
                    {
                        // Should just be able to remove them as you can't actually edit a decal.
                        if (!decals.RemoveDecal(Biome.Owner, decal, decalGrid))
                        {
                            biome.ModifiedTiles.Add(pos.Floored());
                        }
                    }

                    await SuspendIfOutOfTime();

                    foreach (var (index, tile) in loaded.Tiles)
                    {
                        await SuspendIfOutOfTime();

                        if (!maps.TryGetTileRef(Biome.Owner, Biome.Comp1, index, out var tileRef) ||
                            tileRef.Tile != tile)
                        {
                            Biome.Comp2.ModifiedTiles.Add(index);
                            continue;
                        }

                        entities.Clear();
                        var tileBounds = lookup.GetLocalBounds(index, Biome.Comp1.TileSize).Enlarged(-0.05f);

                        lookup.GetEntitiesIntersecting(Biome.Owner,
                            tileBounds,
                            entities);

                        // Still entities remaining so just leave the tile.
                        if (entities.Count > 0)
                        {
                            Biome.Comp2.ModifiedTiles.Add(index);
                            continue;
                        }

                        if (decals.GetDecalsIntersecting(Biome.Owner, tileBounds, component: decalGrid).Count > 0)
                        {
                            Biome.Comp2.ModifiedTiles.Add(index);
                            continue;
                        }

                        // Clear it
                        tiles.Add((index, Tile.Empty));
                    }

                    maps.SetTiles(Biome.Owner, Biome.Comp1, tiles);
                }
            }
        }
        finally
        {
            Biome.Comp2.Loading = false;
        }

        return true;
    }
}
