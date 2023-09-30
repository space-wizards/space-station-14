using System.Diagnostics.Contracts;
using System.Numerics;

namespace Content.Server.Worldgen;

/// <summary>
///     Contains a few world-generation related constants and static functions.
/// </summary>
public static class WorldGen
{
    /// <summary>
    ///     The size of each chunk (isn't that self-explanatory.)
    ///     Be careful about how small you make this.
    /// </summary>
    public const int ChunkSize = 128;

    /// <summary>
    ///     Converts world coordinates to chunk coordinates.
    /// </summary>
    /// <param name="inp">World coordinates</param>
    /// <returns>Chunk coordinates</returns>
    [Pure]
    public static Vector2i WorldToChunkCoords(Vector2i inp)
    {
        return (inp * new Vector2(1.0f / ChunkSize, 1.0f / ChunkSize)).Floored();
    }

    /// <summary>
    ///     Converts world coordinates to chunk coordinates.
    /// </summary>
    /// <param name="inp">World coordinates</param>
    /// <returns>Chunk coordinates</returns>
    [Pure]
    public static Vector2 WorldToChunkCoords(Vector2 inp)
    {
        return inp * new Vector2(1.0f / ChunkSize, 1.0f / ChunkSize);
    }

    /// <summary>
    ///     Converts chunk coordinates to world coordinates.
    /// </summary>
    /// <param name="inp">Chunk coordinates</param>
    /// <returns>World coordinates</returns>
    [Pure]
    public static Vector2 ChunkToWorldCoords(Vector2i inp)
    {
        return inp * ChunkSize;
    }

    /// <summary>
    ///     Converts chunk coordinates to world coordinates.
    /// </summary>
    /// <param name="inp">Chunk coordinates</param>
    /// <returns>World coordinates</returns>
    [Pure]
    public static Vector2 ChunkToWorldCoords(Vector2 inp)
    {
        return inp * ChunkSize;
    }

    /// <summary>
    ///     Converts chunk coordinates to world coordinates, getting the center of the chunk.
    /// </summary>
    /// <param name="inp">Chunk coordinates</param>
    /// <returns>World coordinates</returns>
    [Pure]
    public static Vector2 ChunkToWorldCoordsCentered(Vector2i inp)
    {
        return inp * ChunkSize + Vector2i.One * (ChunkSize / 2);
    }
}

