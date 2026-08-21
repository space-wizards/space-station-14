using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Client.IconSmoothing;

/// <summary>
/// This is used to cache Icon Smoothing data for a grid for the <see cref="IconSmoothComponent"/>
/// This is applied to a grid when an <see cref="IconSmoothComponent"/> entity is anchored to the grid.
/// </summary>
[RegisterComponent]
public sealed partial class IconSmoothGridComponent : Component
{
    /// <summary>
    /// Data for every tile with an anchored <see cref="IconSmoothComponent"/> on the grid.
    /// Stored in Chunks with <see cref="IconChunkData"/> for memory saving.
    /// </summary>
    /// <remarks>
    /// Intentionally not saved.
    /// If you need more than 256 possible different key states, then you may have a problem, change to ushort instead:tm:
    /// </remarks>
    [ViewVariables]
    public readonly Dictionary<Vector2i, IconChunkData> Chunks = new();
}

/// <summary>
/// A simple struct that stores chunk data in a jagged array to be easily retrieved later.
/// Stores a byte which corresponds to a cache for similar <see cref="IconSmoothComponent.Key"/> Hashsets.
/// </summary>
public record struct IconChunkData()
{
    // We use short instead of ushort since I doubt we'll ever need more than 32767 values cached. Plus we need -1 to indicate "needs expansion"
    public short?[] Tiles = new short?[MapGridComponent.DefaultChunkSize * MapGridComponent.DefaultChunkSize];

    public byte Count;

    public bool Empty;

    public bool TryGetTileCache(Vector2i index, [NotNullWhen(true)] out short? cache)
    {
        cache = GetTileCache(index);
        return cache != null;
    }

    public bool TryGetTileCache(int x, int y, [NotNullWhen(true)] out short? cache)
    {
        cache = GetTileCache(x, y);
        return cache != null;
    }

    /// <summary>
    /// Gets the cached value at a given tile on this chunk.
    /// </summary>
    /// <param name="index">Index of our cache, top 4 bits represent Y, bottom for represent X</param>
    /// <param name="cache">The cached value. Not null when true.</param>
    /// <returns>Whether a value existed or not.</returns>
    public bool TryGetTileCache(byte index, [NotNullWhen(true)] out short? cache)
    {
        cache = GetTileCache(index);
        return cache != null;
    }

    public short? GetTileCache(Vector2i index)
    {
        return GetTileCache(index.X, index.Y);
    }

    public short? GetTileCache(int x, int y)
    {
        DebugTools.Assert(x < MapGridComponent.DefaultChunkSize && y < MapGridComponent.DefaultChunkSize, "Vector2i passed exceeded the bounds of our jagged array!!!");
        return GetTileCache((byte)(x + (y << 4)));
    }

    /// <summary>
    /// Gets the cached value at a given tile on this chunk.
    /// </summary>
    /// <param name="index">Index of our cache, top 4 bits represent Y, bottom for represent X</param>
    /// <returns>The cached value</returns>
    public short? GetTileCache(byte index)
    {
        return Tiles[index];
    }

    public void SetTileCache(Vector2i index, short value)
    {
        SetTileCache(index.X, index.Y, value);
    }

    public void SetTileCache(int x, int y, short value)
    {
        DebugTools.Assert(x < MapGridComponent.DefaultChunkSize && y < MapGridComponent.DefaultChunkSize, "Vector2i passed exceeded the bounds of our jagged array!!!");
        SetTileCache((byte)(x + (y << 4)), value);
    }

    /// <summary>
    /// Sets the cached value at a given tile on this chunk.
    /// </summary>
    /// <param name="index">Index of our cache, top 4 bits represent Y, bottom for represent X</param>
    /// <param name="value">Cached value</param>
    public void SetTileCache(byte index, short value)
    {
        DebugTools.Assert(Tiles[index] != null, $"SetTileCache overwrote an empty index without incrementing the ref count!");
        Tiles[index] = value;
        ValidateChunkData();
    }

    public void AddTileCache(Vector2i index, short value)
    {
        AddTileCache(index.X, index.Y, value);
    }

    public void AddTileCache(int x, int y, short value)
    {
        DebugTools.Assert(x < MapGridComponent.DefaultChunkSize && y < MapGridComponent.DefaultChunkSize, "Vector2i passed exceeded the bounds of our jagged array!!!");
        AddTileCache((byte)(x + (y << 4)), value);
    }

    /// <summary>
    /// Adds the cached value at a given tile on this chunk, and increments the number of filled chunks
    /// </summary>
    /// <param name="index">Index of our cache, top 4 bits represent Y, bottom for represent X</param>
    /// <param name="value">Cached value</param>
    public void AddTileCache(byte index, short value)
    {
        DebugTools.Assert(Tiles[index] == null, $"AddTileCache overwrote an existing value, and incremented the cache as if it were empty. Use SetTileCache!");
        Count++;
        Tiles[index] = value;
        ValidateChunkData();
    }

    public void RemoveTileCache(Vector2i index)
    {
        RemoveTileCache(index.X, index.Y);
    }

    public void RemoveTileCache(int x, int y)
    {
        DebugTools.Assert(x < MapGridComponent.DefaultChunkSize && y < MapGridComponent.DefaultChunkSize, "Vector2i passed exceeded the bounds of our jagged array!!!");
        RemoveTileCache((byte)(x + (y << 4)));
    }

    /// <summary>
    /// Clears the cached value at a given tile on this chunk.
    /// </summary>
    /// <param name="index">Index of our cache, top 4 bits represent Y, bottom for represent X</param>
    public void RemoveTileCache(byte index)
    {
        DebugTools.Assert(Tiles[index] != null, $"RemoveTileCache tried to remove a non-existent value!");
        Count--;
        Tiles[index] = null;
        if (Count == 0)
            Empty = true;
        ValidateChunkData();
    }

    private void ValidateChunkData()
    {
        short count = 0;
        foreach (var value in Tiles)
        {
            if (value != null)
                count++;
        }
        DebugTools.Assert(count == 0 == Empty, $"Array was marked as {Empty} despite there being {count} items");
        DebugTools.Assert(count == Count || count == 256 && Count == 0 && !Empty,
            $"Number of cached tiles in this chunk did not match counted tiles counted: {Count} actual: {count}");
    }
}
