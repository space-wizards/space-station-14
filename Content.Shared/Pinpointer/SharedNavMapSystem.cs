using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Pinpointer;

public abstract class SharedNavMapSystem : EntitySystem
{
    public const byte ChunkSize = 4;

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

    [Serializable, NetSerializable]
    protected sealed class NavMapComponentState : ComponentState
    {
        public Dictionary<Vector2i, int> TileData = new();
    }

    [Serializable, NetSerializable]
    protected sealed class NavMapDiffComponentState : ComponentState
    {
        public Dictionary<Vector2i, int> TileData = new();
        public List<Vector2i> RemovedChunks = new();
    }
}
