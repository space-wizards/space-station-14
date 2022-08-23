using System.Runtime.CompilerServices;

namespace Content.Shared.FloodFill.TileFloods;

/// <summary>
///     This is a data structure can be used to ensure the uniqueness of Vector2i indices.
/// </summary>
/// <remarks>
///     This basically exists to replace the use of HashSet{Vector2i} if all you need is the the functions Contains()
///     and Add(). This is both faster and apparently allocates less. Does not support iterating over contents
/// </remarks>
// ReSharper disable once InconsistentNaming
public sealed class UniqueVector2iSet
{
    private const int ChunkSize = 32; // # of bits in an integer.

    private readonly Dictionary<Vector2i, VectorChunk> _chunks = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2i ToChunkIndices(Vector2i indices)
    {
        var x = (int) Math.Floor(indices.X / (float) ChunkSize);
        var y = (int) Math.Floor(indices.Y / (float) ChunkSize);
        return new Vector2i(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(Vector2i index)
    {
        var chunkIndex = ToChunkIndices(index);
        if (_chunks.TryGetValue(chunkIndex, out var chunk))
        {
            return chunk.Add(index);
        }

        chunk = new();
        chunk.Add(index);
        _chunks[chunkIndex] = chunk;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Vector2i index)
    {
        if (!_chunks.TryGetValue(ToChunkIndices(index), out var chunk))
            return false;

        return chunk.Contains(index);
    }

    private sealed class VectorChunk
    {
        // 32*32 chunk represented via 32 ints with 32 bits each. Basic testing showed that this was faster than using
        // 16-sized chunks with ushorts, a bool[,], or just having each chunk be a HashSet.
        private readonly int[] _tiles = new int[ChunkSize];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(Vector2i index)
        {
            var x = MathHelper.Mod(index.X, ChunkSize);
            var y = MathHelper.Mod(index.Y, ChunkSize);

            var oldFlags = _tiles[x];
            var newFlags = oldFlags | (1 << y);

            if (newFlags == oldFlags)
                return false;

            _tiles[x] = newFlags;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vector2i index)
        {
            var x = MathHelper.Mod(index.X, ChunkSize);
            var y = MathHelper.Mod(index.Y, ChunkSize);
            return (_tiles[x] & (1 << y)) != 0;
        }
    }
}
