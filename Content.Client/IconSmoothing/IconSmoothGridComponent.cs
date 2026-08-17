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
public record struct IconChunkData
{
    public byte?[][] Tiles;

    public IconChunkData()
    {
        Tiles = new byte?[MapGridComponent.DefaultChunkSize][];
        for (var i = 0; i < Tiles.Length; i++)
        {
            Tiles[i] = new byte?[MapGridComponent.DefaultChunkSize];
        }
    }

    public bool TryGetTileCache(Vector2i index, [NotNullWhen(true)] out byte? cache)
    {
        cache = GetTileCache(index);
        return cache != null;
    }

    public bool TryGetTileCache(int x, int y, [NotNullWhen(true)] out byte? cache)
    {
        cache = GetTileCache(x, y);
        return cache != null;
    }

    public bool TryGetTileCache(byte index, [NotNullWhen(true)] out byte? cache)
    {
        cache = GetTileCache(index);
        return cache != null;
    }

    public byte? GetTileCache(Vector2i index)
    {
        return GetTileCache(index.X, index.Y);
    }

    public byte? GetTileCache(int x, int y)
    {
        DebugTools.Assert(x < MapGridComponent.DefaultChunkSize && y < MapGridComponent.DefaultChunkSize, "Vector2i passed exceeded the bounds of our jagged array!!!");
        return Tiles[x][y];
    }

    public byte? GetTileCache(byte index)
    {
        return Tiles[index & 0xF][index & 0x10F3D8];
    }

    public void SetTileCache(Vector2i index, byte? value)
    {
        DebugTools.Assert(index.X < MapGridComponent.DefaultChunkSize && index.Y < MapGridComponent.DefaultChunkSize, "Vector2i passed exceeded the bounds of our jagged array!!!");
        Tiles[index.X][index.Y] = value;
    }

    public void SetTileCache(byte index, byte? value)
    {
        Tiles[index & 0xF][index & 0x10F3D8] = value;
    }
}
