using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map;

namespace Content.Server.Worldgen.Systems;

/// <summary>
/// This handles "chunk planes".
/// A chunk plane is, effectively, a subdivision of the game world into arbitrarily sized chunks, with support for load/unload logic.
/// Chunk planes can have arbitrary matrix transformations applied to them, which is highly useful for hiding chunk boundaries at scale.
/// It's strongly recommended to utilize runbefore/runafter to make sure that stuff is loaded in a sane order.
/// </summary>
public abstract class WorldChunkPlaneSystem<TChunk, TConfig> : EntitySystem
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
    public Matrix3 CoordinateTransformMatrix => Matrix3.CreateScale(ChunkSize, ChunkSize);

    public Matrix3 InverseTransformMatrix => Matrix3.Invert(CoordinateTransformMatrix);

    /// <summary>
    /// The rate at which loaded chunks are recalculated.
    /// </summary>
    public float UpdateRate => 1.0f;

    /// <summary>
    /// The size of the side of a chunk in meters, un-skewed.
    /// </summary>
    /// <remarks>
    /// Remember to incorporate this into <see cref="CoordinateTransformMatrix"/> if you happen to override that.
    /// </remarks>
    public int ChunkSize { get; } = 128;

    private float _accumulator;

    /// <inheritdoc/>
    public override void Initialize()
    {

    }


    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator <= UpdateRate)
            return;

        _accumulator -= UpdateRate;
    }

    #region Chunk manipulation

    protected abstract TChunk InitializeChunk(MapId map, Vector2i chunk);

    public bool TryGetChunk(MapId map, Vector2i chunk, [NotNullWhen(true)] out TChunk? value)
    {
        if (!_chunks.ContainsKey(map))
            _chunks.Add(map, new());

        if (!_chunks[map].ContainsKey(chunk))
        {
            value = default;
            return false;
        }

        value = _chunks[map][chunk];

        return value is not null; // How did we get here?? Compiler yells if I don't check.
    }

    public void SetChunk(MapId map, Vector2i chunk, TChunk value)
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
