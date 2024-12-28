using System.Numerics;
using Content.Shared.Decals;
using Robust.Shared.Map;

namespace Content.Shared.Procedural;

/// <summary>
/// Represents the loaded entities, tiles, and decals for a dungeon.
/// </summary>
public sealed class DungeonLoadedData
{
    public static readonly DungeonLoadedData Empty = new();

    public Dictionary<Vector2, EntityUid> Entities = new();

    public Dictionary<Vector2, uint> Decals = new();

    public Dictionary<Vector2, Tile> Tiles = new();
}
