using Content.Shared.Atmos;
using System.Runtime.CompilerServices;

namespace Content.Server.Explosion.EntitySystems;

/// <summary>
///     This class exists to facilitate the iterative neighbor-finding / flooding algorithm used by explosions in <see
///     cref="ExplosionSystem.GetExplosionTiles"/>. This is the base class for <see cref="ExplosionSpaceTileFlood"/> and
///     <see cref="ExplosionGridTileFlood"/>, each of which contains additional code fro logic specific to grids or space.
/// </summary>
/// <remarks>
///     The class stores information about the tiles that the explosion has currently reached, and provides functions to
///     perform a neighbor-finding iteration to expand the explosion area. It also has some functionality that allows
///     tiles to move between grids/space.
/// </remarks>
public abstract class ExplosionTileFlood
{
    // Main tile data sets, mapping iterations onto tile lists
    public Dictionary<int, List<Vector2i>> TileLists = new();
    protected Dictionary<int, List<Vector2i>> BlockedTileLists = new();
    protected Dictionary<int, HashSet<Vector2i>> FreedTileLists = new();

    // The new tile lists added each iteration. I **could** just pass these along to every function, but IMO it is more
    // readable if they are just private variables.
    protected List<Vector2i> NewTiles = default!;
    protected List<Vector2i> NewBlockedTiles = default!;
    protected HashSet<Vector2i> NewFreedTiles = default!;

    // HashSets used to ensure uniqueness of tiles. Prevents the explosion from looping back in on itself.
    protected UniqueVector2iSet ProcessedTiles = new();
    protected UniqueVector2iSet UnenteredBlockedTiles = new();
    protected UniqueVector2iSet EnteredBlockedTiles = new();

    public abstract void InitTile(Vector2i initialTile);

    protected abstract void ProcessNewTile(int iteration, Vector2i tile, AtmosDirection entryDirections);

    protected abstract AtmosDirection GetUnblockedDirectionOrAll(Vector2i tile);

    protected void AddNewDiagonalTiles(int iteration, IEnumerable<Vector2i> tiles, bool ignoreLocalBlocker = false)
    {
        AtmosDirection entryDirection = AtmosDirection.Invalid;
        foreach (var tile in tiles)
        {
            var freeDirections = ignoreLocalBlocker ? AtmosDirection.All : GetUnblockedDirectionOrAll(tile);

            // Get the free directions of the directly adjacent tiles
            var freeDirectionsN = GetUnblockedDirectionOrAll(tile.Offset(AtmosDirection.North));
            var freeDirectionsE = GetUnblockedDirectionOrAll(tile.Offset(AtmosDirection.East));
            var freeDirectionsS = GetUnblockedDirectionOrAll(tile.Offset(AtmosDirection.South));
            var freeDirectionsW = GetUnblockedDirectionOrAll(tile.Offset(AtmosDirection.West));

            // North East
            if (freeDirections.IsFlagSet(AtmosDirection.North) && freeDirectionsN.IsFlagSet(AtmosDirection.SouthEast))
                entryDirection |= AtmosDirection.West;

            if (freeDirections.IsFlagSet(AtmosDirection.East) && freeDirectionsE.IsFlagSet(AtmosDirection.NorthWest))
                entryDirection |= AtmosDirection.South;

            if (entryDirection != AtmosDirection.Invalid)
            {
                ProcessNewTile(iteration, tile + (1, 1), entryDirection);
                entryDirection = AtmosDirection.Invalid;
            }

            // North West
            if (freeDirections.IsFlagSet(AtmosDirection.North) && freeDirectionsN.IsFlagSet(AtmosDirection.SouthWest))
                entryDirection |= AtmosDirection.East;

            if (freeDirections.IsFlagSet(AtmosDirection.West) && freeDirectionsW.IsFlagSet(AtmosDirection.NorthEast))
                entryDirection |= AtmosDirection.West;

            if (entryDirection != AtmosDirection.Invalid)
            {
                ProcessNewTile(iteration, tile + (-1, 1), entryDirection);
                entryDirection = AtmosDirection.Invalid;
            }

            // South East
            if (freeDirections.IsFlagSet(AtmosDirection.South) && freeDirectionsS.IsFlagSet(AtmosDirection.NorthEast))
                entryDirection |= AtmosDirection.West;

            if (freeDirections.IsFlagSet(AtmosDirection.East) && freeDirectionsE.IsFlagSet(AtmosDirection.SouthWest))
                entryDirection |= AtmosDirection.North;

            if (entryDirection != AtmosDirection.Invalid)
            {
                ProcessNewTile(iteration, tile + (1, -1), entryDirection);
                entryDirection = AtmosDirection.Invalid;
            }

            // South West
            if (freeDirections.IsFlagSet(AtmosDirection.South) && freeDirectionsS.IsFlagSet(AtmosDirection.NorthWest))
                entryDirection |= AtmosDirection.West;

            if (freeDirections.IsFlagSet(AtmosDirection.West) && freeDirectionsW.IsFlagSet(AtmosDirection.SouthEast))
                entryDirection |= AtmosDirection.North;

            if (entryDirection != AtmosDirection.Invalid)
            {
                ProcessNewTile(iteration, tile + (-1, -1), entryDirection);
                entryDirection = AtmosDirection.Invalid;
            }
        }
    }

    /// <summary>
    ///     Merge all tile lists into a single output tile list.
    /// </summary>
    public void CleanUp()
    {
        foreach (var (iteration, blocked) in BlockedTileLists)
        {
            if (TileLists.TryGetValue(iteration, out var tiles))
                tiles.AddRange(blocked);
            else
                TileLists[iteration] = blocked;
        }
    }
}

/// <summary>
///     This is a data structure can be used to ensure the uniqueness of Vector2i indices.
/// </summary>
/// <remarks>
///     This basically exists to replace the use of HashSet&lt;Vector2i&gt; if all you need is the the functions Contains()
///     and Add(). This is both faster and apparently allocates less. Does not support iterating over contents
/// </remarks>
public sealed class UniqueVector2iSet
{
    private const int ChunkSize = 32; // # of bits in an integer.

    private Dictionary<Vector2i, VectorChunk> _chunks = new();

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
