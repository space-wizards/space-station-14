using System.Linq;
using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Tag;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Pinpointer;

public abstract class SharedNavMapSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public const byte ChunkSize = 4;

    public readonly NavMapChunkType[] EntityChunkTypes =
    {
        NavMapChunkType.Invalid,
        NavMapChunkType.Wall,
        NavMapChunkType.Airlock,
    };

    private readonly string[] _wallTags = ["Wall", "Window"];

    /// <summary>
    /// Converts the chunk's tile into a bitflag for the slot.
    /// </summary>
    public static int GetFlag(Vector2i relativeTile)
    {
        return 1 << (relativeTile.X * ChunkSize + relativeTile.Y);
    }

    /// <summary>
    /// Converts the chunk's tile into a bitflag for the slot.
    /// </summary>
    public static Vector2i GetTile(int flag)
    {
        var value = Math.Log2(flag);
        var x = (int) value / ChunkSize;
        var y = (int) value % ChunkSize;
        var result = new Vector2i(x, y);

        DebugTools.Assert(GetFlag(result) == flag);

        return new Vector2i(x, y);
    }

    public NavMapChunk SetAllEdgesForChunkTile(NavMapChunk chunk, Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);
        var flag = (ushort) GetFlag(relative);

        foreach (var (direction, _) in chunk.TileData)
            chunk.TileData[direction] |= flag;

        return chunk;
    }

    public NavMapChunk UnsetAllEdgesForChunkTile(NavMapChunk chunk, Vector2i tile)
    {
        var relative = SharedMapSystem.GetChunkRelative(tile, ChunkSize);
        var flag = (ushort) GetFlag(relative);
        var invFlag = (ushort) ~flag;

        foreach (var (direction, _) in chunk.TileData)
            chunk.TileData[direction] &= invFlag;

        return chunk;
    }

    public ushort GetCombinedEdgesForChunk(Dictionary<AtmosDirection, ushort> tile)
    {
        ushort combined = 0;

        foreach (var (_, value) in tile)
            combined |= value;

        return combined;
    }

    public bool AllTileEdgesAreOccupied(Dictionary<AtmosDirection, ushort> tileData, Vector2i tile)
    {
        var flag = (ushort) GetFlag(tile);

        foreach (var (direction, _) in tileData)
        {
            if ((tileData[direction] & flag) == 0)
                return false;
        }

        return true;
    }

    public NavMapChunkType GetAssociatedEntityChunkType(EntityUid uid)
    {
        var category = NavMapChunkType.Invalid;

        if (HasComp<NavMapDoorComponent>(uid))
            category = NavMapChunkType.Airlock;

        else if (_tagSystem.HasAnyTag(uid, _wallTags))
            category = NavMapChunkType.Wall;

        return category;
    }

    #region: System messages

    [Serializable, NetSerializable]
    protected sealed class NavMapComponentState : ComponentState
    {
        public Dictionary<(NavMapChunkType, Vector2i), Dictionary<AtmosDirection, ushort>> ChunkData = new();
        public List<NavMapBeacon> Beacons = new();
    }

    [Serializable, NetSerializable]
    public readonly record struct NavMapBeacon(NetEntity NetEnt, Color Color, string Text, Vector2 Position);

    [Serializable, NetSerializable]
    public sealed class NavMapChunkChangedEvent : EntityEventArgs
    {
        public NetEntity Grid;
        public NavMapChunkType Category;
        public Vector2i ChunkOrigin;
        public Dictionary<AtmosDirection, ushort> TileData;

        public NavMapChunkChangedEvent(NetEntity grid, NavMapChunkType category, Vector2i chunkOrigin, Dictionary<AtmosDirection, ushort> tileData)
        {
            Grid = grid;
            Category = category;
            ChunkOrigin = chunkOrigin;
            TileData = tileData;
        }
    };

    [Serializable, NetSerializable]
    public sealed class NavMapBeaconChangedEvent : EntityEventArgs
    {
        public NetEntity Grid;
        public NavMapBeacon Beacon;

        public NavMapBeaconChangedEvent(NetEntity grid, NavMapBeacon beacon)
        {
            Grid = grid;
            Beacon = beacon;
        }
    };

    #endregion
}
