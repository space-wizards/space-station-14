using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Procedural;
using Content.Shared.CCVar;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Box2i = Robust.Shared.Maths.Box2i;

namespace Content.Server.Parallax;

public sealed partial class NewBiomeSystem : EntitySystem
{
    /*
     * Handles loading in biomes around players.
     * These are essentially chunked-areas that load in dungeons and can also be unloaded.
     */

    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    /// <summary>
    /// Jobs for biomes to load.
    /// </summary>
    private JobQueue _biomeQueue = default!;

    private float _checkUnloadAccumulator;
    private float _checkUnloadTime;
    private float _loadRange = 1f;
    private float _loadTime;

    private EntityQuery<NewBiomeComponent> _biomeQuery;

    public override void Initialize()
    {
        base.Initialize();
        _biomeQuery = GetEntityQuery<NewBiomeComponent>();

        Subs.CVar(_cfgManager, CCVars.BiomeLoadRange, OnLoadRange);
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
        foreach (var player in new List<ICommonSession>())
        {
            if (player.AttachedEntity != null)
            {
                TryAddBiomeBounds(player.AttachedEntity.Value);
            }

            // If not relevant then discard.
            foreach (var viewer in player.ViewSubscriptions)
            {
                TryAddBiomeBounds(viewer);
            }
        }

        // Check if any biomes are intersected and queue up loads.
        while (query.MoveNext(out var biome))
        {
            if (biome.Loading || biome.LoadedBounds.Count == 0)
                continue;

            biome.Loading = true;
            var job = new BiomeLoadJob(_loadTime)
            {
                Biome = biome,
            };
            _biomeQueue.EnqueueJob(job);
        }

        _checkUnloadAccumulator += frameTime;

        if (_checkUnloadAccumulator > _checkUnloadTime)
        {
            // Work out chunks not in range and unload.
            UnloadChunks();
            // Don't care about eating the remainder here.
            _checkUnloadTime = 0f;
        }

        // Process jobs.
        _biomeQueue.Process();
    }

    private void UnloadChunks()
    {
        var query = AllEntityQuery<NewBiomeComponent>();

        while (query.MoveNext(out var biome))
        {
            // Only start unloading if it's currently not loading anything.
            if (biome.Loading)
                continue;

            foreach (var (layerId, loadedLayer) in biome.LoadedData)
            {
                var layer = biome.Layers[layerId];
                var toUnload = new ValueList<Vector2i>();

                // Go through each loaded chunk and check if they can be unloaded by checking if any players are in range.
                foreach (var chunk in loadedLayer.Keys)
                {
                    // If it's pending then don't interrupt the loading
                    if (biome.PendingData.TryGetValue(layerId, out var pending) && pending.Contains(chunk))
                        continue;

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

                    toUnload.Add(chunk);
                }

                if (toUnload.Count == 0)
                    continue;

                // Queue up unloads.
                var job = new BiomeUnloadJob(_loadTime);
                _biomeQueue.EnqueueJob(job);
            }
        }
    }

    /// <summary>
    /// Gets the full bounds to be loaded. Considers layer dependencies where they may have different chunk sizes.
    /// </summary>
    private Box2i GetFullBounds(NewBiomeComponent component, Box2i bounds)
    {
        var baseBounds = bounds;

        foreach (var layer in component.Layers.Values)
        {
            var layerBounds = baseBounds;

            if (layer.DependsOn != null)
            {
                foreach (var sub in layer.DependsOn)
                {
                    var depLayer = component.Layers[sub];

                    layerBounds = layerBounds.Union(GetLayerBounds(depLayer, layerBounds));
                }
            }

            bounds = bounds.Union(layerBounds);
        }

        return bounds;
    }

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

        biome.LoadedBounds.Add(bounds);
    }

    public Box2i GetLayerBounds(NewBiomeMetaLayer layer, Box2i layerBounds)
    {
        var chunkSize = (Vector2) layer.Size;

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
        private NewBiomeSystem System = default!;

        public Entity<MapGridComponent> Grid;

        /// <summary>
        /// Biome that is getting loaded.
        /// </summary>
        public NewBiomeComponent Biome = default!;

        public BiomeLoadJob(double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
        }

        public BiomeLoadJob(double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
        {
        }

        protected override async Task<bool> Process()
        {
            foreach (var bound in Biome.LoadedBounds)
            {
                foreach (var (layerId, layer) in Biome.Layers)
                {
                    await LoadLayer(layerId, layer, bound);
                }
            }

            // Finished
            DebugTools.Assert(Biome.Loading);
            Biome.Loading = false;
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
                var layerLoaded = Biome.LoadedData.GetOrNew(layerId);

                // Layer already loaded for this chunk.
                // This can potentially happen if we're moving and the player's bounds changed but some existing chunks remain.
                if (layerLoaded.ContainsKey(chunk.Value))
                {
                    continue;
                }

                var layerPending = Biome.PendingData.GetOrNew(layerId);
                DebugTools.Assert(!layerPending.Contains(chunk.Value));

                layerPending.Add(chunk.Value);

                // Start loading here.
                var loadedData = new BiomeLoadedData()
                {

                };

                unchecked
                {
                    var seedOffset = chunk.Value.X * 256 + chunk.Value.Y + Biome.Seed;

                    // Load dungeon here async await and all that jaz.
                    await IoCManager.Resolve<IEntityManager>()
                        .System<DungeonSystem>()
                        .GenerateDungeonAsync(layer.Dungeon, Grid.Owner, Grid.Comp, chunk.Value, seedOffset);
                }

                // Cleanup loading
                layerPending.Remove(chunk.Value);
                layerLoaded.Add(chunk.Value, loadedData);
            }
        }
    }

    public sealed class BiomeUnloadJob : Job<bool>
    {
        public List<Vector2i> Chunks = new();

        public BiomeUnloadJob(double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
        }

        public BiomeUnloadJob(double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
        {
        }

        protected override Task<bool> Process()
        {
            //
        }
    }
