using Content.Server.Worldgen.Components;
using Robust.Shared.Map;

namespace Content.Server.Worldgen.Systems;

/// <summary>
/// This handles "chunk planes".
/// A chunk plane is, effectively, a subdivision of the game world into arbitrarily sized chunks, with support for load/unload logic.
/// Chunk planes can have arbitrary matrix transformations applied to them, which is highly useful for hiding chunk boundaries at scale,
/// for example making your biomes plane skewed compared to the structures/debris plane can help hide biome boundaries. (and biomes can be more fine-grained than those planes)
/// It's strongly recommended to utilize runbefore/runafter to make sure that stuff is loaded in a sane order.
/// </summary>
public abstract partial class WorldChunkPlaneSystem<TChunk, TConfig> : EntitySystem
    where TChunk : new()
    where TConfig: new()
{
    private Dictionary<MapId, Dictionary<Vector2i, TChunk>> _chunks = new();

    protected Dictionary<MapId, TConfig> MapConfigurations = new();

    /// <summary>
    /// The matrix to use to convert chunk-space coordinates into world-space.
    /// </summary>
    /// <remarks>
    /// Chunk size needs to be incorporated into this as the scale, unless you like 1m x 1m chunks eating all your RAM and CPU.
    /// Easiest way to do this is to take `base.CoordinateTransformMatrix` and then transform that.
    /// </remarks>
    public virtual Matrix3 CoordinateTransformMatrix => Matrix3.CreateScale(ChunkSize, ChunkSize);

    public Matrix3 InverseTransformMatrix => Matrix3.Invert(CoordinateTransformMatrix);

    /// <summary>
    /// The rate at which loaded chunks are recalculated. Can be set to null to disable chunkloading.
    /// </summary>
    public virtual float? UpdateRate => 1.0f;

    /// <summary>
    /// The size of the side of a chunk in meters, un-skewed.
    /// </summary>
    /// <remarks>
    /// Remember to incorporate this into <see cref="CoordinateTransformMatrix"/> if you happen to override that.
    /// </remarks>
    public virtual int ChunkSize => 128;

    /// <summary>
    /// How many extra grid chunks will be loaded that are technically out-of-range.
    /// </summary>
    public virtual int LoadingCheatFactor => 1;

    private float _accumulator;
    private readonly Dictionary<MapId,HashSet<Vector2i>> _loadedChunks = new();



    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    protected virtual void UnloadChunk(MapId map, Vector2i chunk)
    {
        Logger.Debug($"Unloaded {chunk} on {map}");
    }

    protected virtual void LoadChunk(MapId map, Vector2i chunk)
    {
        Logger.Debug($"Loaded {chunk} on {map}");
    }

    public override void Update(float frameTime)
    {
        if (UpdateRate is null)
            return; // We don't update.

        _accumulator += frameTime;
        if (_accumulator <= UpdateRate.Value)
            return;

        _accumulator -= UpdateRate.Value;

        var inv = MathF.Sqrt((float)InverseTransformMatrix.Determinant);

        var toLoad = new Dictionary<MapId, HashSet<Vector2i>>();

        foreach (var (loader, xform) in EntityQuery<ChunkLoaderComponent, TransformComponent>())
        {
            //if (!MapConfigurations.ContainsKey(xform.MapID))
            //    continue;

            // TODO: Triple-check this crazy logic does what's expected of it:
            // This is to try and find every grid square within a circle, however said grid has been transformed by some arbitrary invertable Matrix3 so I have to suffer a bit.
            // "I think you can transform the center of the circle, then transform the distance function and add the cells until they no longer meet the condition (<=radius)" << TRY THIS LATER
            var radius = inv * loader.Range;
            if (!toLoad.ContainsKey(xform.MapID))
                toLoad.Add(xform.MapID, new());
            if (!_loadedChunks.ContainsKey(xform.MapID))
                _loadedChunks.Add(xform.MapID, new());

            var nearby = ChunksNear(InverseTransformMatrix.Transform(xform.WorldPosition).Floored(), (int)Math.Ceiling(radius));

            var circle = new Circle(xform.WorldPosition, MathF.Ceiling(loader.Range / ChunkSize + LoadingCheatFactor) * ChunkSize);

            foreach (var position in nearby)
            {
                var worldPos = CoordinateTransformMatrix.Transform(position);
                if (circle.Contains(worldPos))
                    toLoad[xform.MapID].Add(position);
            }
        }

        var newChunks = new Dictionary<MapId, HashSet<Vector2i>>();
        foreach (var (map, loadedChunks) in _loadedChunks)
        {
            newChunks[map] = new(toLoad[map]);
            newChunks[map].ExceptWith(loadedChunks);
        }
        var unloadChunks = new Dictionary<MapId, HashSet<Vector2i>>();
        foreach (var (map, loadedChunks) in _loadedChunks)
        {
            unloadChunks[map] = new(loadedChunks);
            unloadChunks[map].ExceptWith(toLoad[map]);
        }

        foreach (var (map, chunks) in unloadChunks)
        {
            foreach (var chunk in chunks)
            {
                UnloadChunk(map, chunk);
            }
        }

        foreach (var (map, chunks) in newChunks)
        {
            foreach (var chunk in chunks)
            {
                LoadChunk(map, chunk);
            }
        }

        foreach (var (map, loadedChunks) in _loadedChunks)
        {
            loadedChunks.Clear();
            loadedChunks.UnionWith(toLoad[map]);
        }
    }

    private IEnumerable<Vector2i> ChunksNear(Vector2i position, int radius)
    {
        for (var x = -radius; x <= radius; x+=1)
        {
            for (var y = -radius; y <= radius; y+=1)
            {
                if (x * x + y * y <= radius * radius)
                {
                    yield return position + (x, y);
                }
            }
        }
    }

    #region Chunk manipulation

    protected abstract TChunk InitializeChunk(MapId map, Vector2i chunk);

    public TChunk GetChunk(MapId map, Vector2i chunk)
    {
        if (!_chunks.ContainsKey(map))
            _chunks.Add(map, new());

        if (!_chunks[map].ContainsKey(chunk))
            _chunks[map][chunk] = InitializeChunk(map, chunk);

        return _chunks[map][chunk];
    }

    protected void SetChunk(MapId map, Vector2i chunk, TChunk value)
    {
        if (!_chunks.ContainsKey(map))
            _chunks.Add(map, new());

        _chunks[map][chunk] = value;
    }

    public Vector2 ChunkSpaceToWorld(Vector2 chunkCoords)
    {
        return CoordinateTransformMatrix.Transform(chunkCoords);
    }

    public Vector2 WorldSpaceToChunkSpace(Vector2 worldCoords)
    {
        return InverseTransformMatrix.Transform(worldCoords);
    }

    public Vector2i GetChunkAt(Vector2 worldCoords)
    {
        return InverseTransformMatrix.Transform(worldCoords).Floored();
    }

    public Vector2 GetChunkCenterWorld(Vector2i chunkCoords)
    {
        return CoordinateTransformMatrix.Transform(chunkCoords);
    }

    #endregion

    #region Generation manipulation

    /// <summary>
    /// Attempts to clear out the given portion of the world.
    /// </summary>
    /// <param name="area">The area to clear in world-space.</param>
    /// <returns>Whether or not clearing was a success.</returns>
    public abstract bool TryClearWorldSpace(Box2Rotated area);

    /// <inheritdoc cref="TryClearWorldSpace(Robust.Shared.Maths.Box2Rotated)"/>
    public abstract bool TryClearWorldSpace(Circle area);

    #endregion
}
