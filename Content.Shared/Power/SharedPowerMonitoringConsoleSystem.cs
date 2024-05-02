using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Content.Shared.Power;

[UsedImplicitly]
public abstract class SharedPowerMonitoringConsoleSystem : EntitySystem
{
    // Chunk size is limited as we require ChunkSize^2 <= 32 (number of bits in an int)
    public const int ChunkSize = 5;

    /// <summary>
    /// Converts the chunk's tile into a bitflag for the slot.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFlag(Vector2i relativeTile)
    {
        return 1 << (relativeTile.X * ChunkSize + relativeTile.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetTileFromIndex(int index)
    {
        var x = index / ChunkSize;
        var y = index % ChunkSize;
        return new Vector2i(x, y);
    }
}
