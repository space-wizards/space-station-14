using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Decals;
using Content.Server.Shuttles.Events;
using Content.Shared.CCVar;
using Content.Shared.Decals;
using Content.Shared.Ghost;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Components;
using Content.Shared.Sprite;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Procedural;

public sealed partial class BiomeSystem : EntitySystem
{
    /*
     * Handles loading in biomes around players.
     * These are essentially chunked-areas that load in dungeons and can also be unloaded.
     */

    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _protomanager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    /// <summary>
    /// Ignored components for default checks
    /// </summary>
    public static readonly List<string> IgnoredComponents = new();

    /// <summary>
    /// Jobs for biomes to load.
    /// </summary>
    private JobQueue _biomeQueue = default!;

    private float _loadRange = 1f;
    private float _loadTime;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<BiomeComponent> _biomeQuery;

    private static readonly ProtoId<TagPrototype> AllowBiomeLoadingTag = "AllowBiomeLoading";

    public override void Initialize()
    {
        base.Initialize();
        _ghostQuery = GetEntityQuery<GhostComponent>();
        _biomeQuery = GetEntityQuery<BiomeComponent>();

        IgnoredComponents.Add(Factory.GetComponentName<RandomSpriteComponent>());

        Subs.CVar(_cfgManager, CCVars.BiomeLoadRange, OnLoadRange, true);
        Subs.CVar(_cfgManager, CCVars.BiomeLoadTime, OnLoadTime, true);

        SubscribeLocalEvent<FTLStartedEvent>(OnFTLStarted);
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

    private void OnFTLStarted(ref FTLStartedEvent ev)
    {
        var targetMap = _xforms.ToMapCoordinates(ev.TargetCoordinates);
        var targetMapUid = _maps.GetMapOrInvalid(targetMap.MapId);

        if (!TryComp<BiomeComponent>(targetMapUid, out var biome))
            return;

        var preloadArea = new Vector2(32f, 32f);
        var targetArea = new Box2(targetMap.Position - preloadArea, targetMap.Position + preloadArea);
        Preload(targetMapUid, biome, (Box2i) targetArea);
    }

    /// <summary>
    /// Preloads biome for the specified area.
    /// </summary>
    public void Preload(EntityUid uid, BiomeComponent component, Box2i area)
    {
        component.PreloadAreas.Add(area);
    }

    private bool CanLoad(EntityUid uid)
    {
        return !_ghostQuery.HasComp(uid) || _tags.HasTag(uid, AllowBiomeLoadingTag);
    }

    public void AddLayer(Entity<BiomeComponent?> biome, string label, BiomeMetaLayer layer)
    {
        if (!Resolve(biome.Owner, ref biome.Comp))
            return;

        if (!biome.Comp.Layers.TryAdd(label, layer))
        {
            Log.Warning($"Tried to add layer {label} to biome {ToPrettyString(biome)} that already has it?");
            return;
        }
    }

    public void AddLayer(Entity<BiomeComponent?> biome, string label, ProtoId<DungeonConfigPrototype> layer)
    {
        if (!Resolve(biome.Owner, ref biome.Comp))
            return;

        var metaLayer = new BiomeMetaLayer()
        {
            Dungeon = layer,
        };

        AddLayer(biome, label, metaLayer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<BiomeComponent>();

        while (query.MoveNext(out var biome))
        {
            // If it's still loading then don't touch the observer bounds.
            if (biome.Loading)
                continue;

            biome.LoadedBounds.Clear();

            // Make sure preloads go in.
            foreach (var preload in biome.PreloadAreas)
            {
                biome.LoadedBounds.Add(preload);
            }
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
        UnloadChunks();

        var loadQuery = AllEntityQuery<BiomeComponent, MapGridComponent>();

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
        var query = AllEntityQuery<BiomeComponent, MapGridComponent>();

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
                        // Give a buffer range so we don't immediately unload if we wiggle, we'll just double the load area.
                        var enlarged = playerView.Enlarged((int) _loadRange);

                        // Still relevant
                        if (chunkBounds.Intersects(enlarged))
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
    private Box2i GetFullBounds(BiomeComponent component, Box2i bounds)
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
        if (!CanLoad(uid))
            return;

        var xform = Transform(uid);

        // No biome to load
        if (!_biomeQuery.TryComp(xform.MapUid, out var biome))
            return;

        // Currently already loading.
        if (biome.Loading)
            return;

        var center = _xforms.GetWorldPosition(uid);

        var bounds = new Box2i((center - new Vector2(_loadRange, _loadRange)).Floored(), (center + new Vector2(_loadRange, _loadRange)).Floored());

        // If it's moving then preload in that direction
        if (TryComp(uid, out PhysicsComponent? physics))
        {
            bounds = bounds.Union(bounds.Translated((physics.LinearVelocity * 2f).Floored()));
        }

        var adjustedBounds = GetFullBounds(biome, bounds);
        biome.LoadedBounds.Add(adjustedBounds);
    }

    public Box2i GetLayerBounds(BiomeMetaLayer layer, Box2i layerBounds)
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
     private BiomeSystem System = default!;

    public Entity<MapGridComponent> Grid;

    /// <summary>
    /// Biome that is getting loaded.
    /// </summary>
    public BiomeComponent Biome = default!;

    public BiomeLoadJob(double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        IoCManager.InjectDependencies(this);
        System = _entManager.System<BiomeSystem>();
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

    private async Task LoadLayer(string layerId, BiomeMetaLayer layer, Box2i parentBounds)
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
        var chunkEnumerator = new NearestChunkEnumerator(loadBounds, layer.Size);

        while (chunkEnumerator.MoveNext(out var chunk))
        {
            var chunkOrigin = chunk.Value;
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

    public Entity<MapGridComponent, BiomeComponent> Biome;
    public Dictionary<string, List<Vector2i>> ToUnload = default!;

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
                        if (pos == xform.LocalPosition.Floored() && xform.GridUid == Biome.Owner && _entManager.IsDefault(ent, BiomeSystem.IgnoredComponents))
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

                        if (Biome.Comp2.ModifiedTiles.Contains(index))
                        {
                            continue;
                        }

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

        // If we have any preloads then mark those as modified so they persist.
        foreach (var preload in Biome.Comp2.PreloadAreas)
        {
            for (var x = preload.Left; x <= preload.Right; x++)
            {
                for (var y = preload.Bottom; y <= preload.Top; y++)
                {
                    var index = new Vector2i(x, y);
                    Biome.Comp2.ModifiedTiles.Add(index);
                }
            }

            await SuspendIfOutOfTime();
        }

        return true;
    }
}
