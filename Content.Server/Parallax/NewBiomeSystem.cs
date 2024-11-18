using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Box2i = Robust.Shared.Maths.Box2i;

namespace Content.Server.Parallax;

public sealed class NewBiomeSystem : EntitySystem
{
    /*
     * Handles loading in biomes around players.
     * Separate but similar to dungeons.
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

    /// <summary>
    /// Only load 1 job per entity at a time.
    /// </summary>
    private readonly Dictionary<EntityUid, BiomeLoadJob> _loadingJobs = new();

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
            biome.LoadedBounds.Clear();
        }

        // Get all relevant players.
        // If they already have a job loading then don't make a new one yet.
        foreach (var player in new List<ICommonSession>())
        {
            if (player.AttachedEntity != null)
            {
                TryAddBiomeJob(player.AttachedEntity.Value);
            }

            // If not relevant then discard.
            foreach (var viewer in player.ViewSubscriptions)
            {
                TryAddBiomeJob(viewer);
            }
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
            foreach (var (layerId, loadedLayer) in biome.LoadedData)
            {
                var layer = biome.Layers[layerId];
                var toUnload = new ValueList<Vector2i>();

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
                var job = new BiomeUnloadJob();
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

    private void TryAddBiomeJob(EntityUid uid)
    {
        var xform = Transform(uid);

        // No biome to load
        if (!_biomeQuery.TryComp(xform.MapUid, out var biome))
            return;

        if (_loadingJobs.ContainsKey(uid))
            return;

        var job = new BiomeLoadJob(_loadTime)
        {
            Biome = biome,
        };

        var center = _xforms.GetWorldPosition(uid);

        job.Bounds = new Box2i((center - new Vector2(_loadRange, _loadRange)).Floored(), (center + new Vector2(_loadRange, _loadRange)).Floored());

        biome.LoadedBounds.Add(job.Bounds);

        _loadingJobs.Add(uid, job);
        _biomeQueue.EnqueueJob(job);
    }

    private Box2i GetLayerBounds(NewBiomeMetaLayer layer, Box2i layerBounds)
    {
        var chunkSize = (Vector2) layer.Size;

        // Need to round the bounds to our chunk size to ensure we load whole chunks.
        // We also need to know the minimum bounds for our dependencies to load.
        var layerBL = (layerBounds.BottomLeft / chunkSize).Floored() * chunkSize;
        var layerTR = (layerBounds.TopRight / chunkSize).Ceiled() * chunkSize;

        var loadBounds = new Box2i(layerBL.Floored(), layerTR.Ceiled());
        return loadBounds;
    }

    private sealed class BiomeLoadJob : Job<bool>
    {
        /// <summary>
        /// Biome that is getting loaded.
        /// </summary>
        public NewBiomeComponent Biome;

        /// <summary>
        /// Bounds to load in. The actual area may be loaded larger due to layer dependencies.
        /// </summary>
        public Box2i Bounds;

        public BiomeLoadJob(double maxTime, CancellationToken cancellation = default) : base(maxTime, cancellation)
        {
        }

        public BiomeLoadJob(double maxTime, IStopwatch stopwatch, CancellationToken cancellation = default) : base(maxTime, stopwatch, cancellation)
        {
        }

        protected override async Task<bool> Process()
        {
            foreach (var (layerId, layer) in Biome.Layers)
            {
                await LoadLayer(layerId, layer, Bounds);
            }

            return true;
        }

        private async Task LoadLayer(string layerId, NewBiomeMetaLayer layer, Box2i parentBounds)
        {
            var loadBounds = GetLayerBounds(layer, parentBounds);

            // Make sure our dependencies are loaded first.
            if (layer.DependsOn != null)
            {
                foreach (var sub in layer.DependsOn)
                {
                    LoadLayer(sub, loadBounds);
                }
            }

            // Okay all of our dependencies loaded so we can send it.
            var chunkEnumerator = new ChunkIndicesEnumerator(loadBounds, layer.Size);

            while (chunkEnumerator.MoveNext(out var chunk))
            {
                var layerLoaded = Biome.LoadedData.GetOrNew(layerId);

                // Layer already loaded for this chunk.
                if (layerLoaded.ContainsKey(chunk.Value))
                {
                    continue;
                }

                var layerPending = Biome.PendingData.GetOrNew(layerId);
                DebugTools.Assert(!layerPending.Contains(chunk.Value));
                layerPending.Add(chunk.Value);

                // Load here
                foreach (var sub in layer.SubLayers)
                {

                }

                // Cleanup loading
                layerPending.Remove(chunk.Value);
                layerLoaded.Add(chunk.Value, new BiomeLoadedData()
                {

                });
            }
        }
    }

    private sealed class BiomeUnloadJob : Job<bool>
    {
        public List<Vector2i> Chunks = new();
    }
}
