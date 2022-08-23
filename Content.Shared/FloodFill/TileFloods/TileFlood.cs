using Content.Shared.Atmos;

namespace Content.Shared.FloodFill.TileFloods;

/// <summary>
///     This class exists to facilitate the iterative neighbor-finding / flooding algorithm used by explosions
///     and other systems. This is the base class for <see cref="SpaceTileFlood"/> and
///     <see cref="GridTileFlood"/>, each of which contains additional code fro logic specific to grids or space.
/// </summary>
/// <remarks>
///     The class stores information about the tiles that the flood has currently reached, and provides functions to
///     perform a neighbor-finding iteration to expand the flood area. It also has some functionality that allows
///     tiles to move between grids/space.
/// </remarks>
public abstract class TileFlood
{
    // Main tile data sets, mapping iterations onto tile lists
    public readonly Dictionary<int, List<Vector2i>> TileLists = new();
    protected readonly Dictionary<int, List<Vector2i>> BlockedTileLists = new();
    protected readonly Dictionary<int, HashSet<Vector2i>> FreedTileLists = new();

    // The new tile lists added each iteration. I **could** just pass these along to every function, but IMO it is more
    // readable if they are just private variables.
    protected List<Vector2i> NewTiles = default!;
    protected List<Vector2i> NewBlockedTiles = default!;
    protected HashSet<Vector2i> NewFreedTiles = default!;

    // HashSets used to ensure uniqueness of tiles. Prevents the explosion from looping back in on itself.
    protected readonly UniqueVector2iSet ProcessedTiles = new();
    protected readonly UniqueVector2iSet UnenteredBlockedTiles = new();
    protected readonly UniqueVector2iSet EnteredBlockedTiles = new();

    public abstract void InitTile(Vector2i initialTile);

    protected abstract void ProcessNewTile(int iteration, Vector2i tile, AtmosDirection entryDirections);

    protected abstract AtmosDirection GetUnblockedDirectionOrAll(Vector2i tile);

    protected void AddNewDiagonalTiles(int iteration, IEnumerable<Vector2i> tiles, bool ignoreLocalBlocker = false)
    {
        var entryDirection = AtmosDirection.Invalid;
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
