using System.Linq;
using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared.Procedural;

/// <summary>
/// Contains the loaded data for a dungeon.
/// </summary>
[DataDefinition]
public sealed partial class DungeonData
{
    [DataField]
    public Dictionary<uint, Vector2> Decals = new();

    [DataField]
    public Dictionary<EntityUid, Vector2i> Entities = new();

    [DataField]
    public Dictionary<Vector2i, Tile> Tiles = new();

    public static DungeonData Empty = new();

    public void Merge(DungeonData data)
    {
        foreach (var did in data.Decals)
        {
            Decals[did.Key] = did.Value;
        }

        foreach (var ent in data.Entities)
        {
            Entities[ent.Key] = ent.Value;
        }

        foreach (var tile in data.Tiles)
        {
            Tiles[tile.Key] = tile.Value;
        }
    }
}
