using Content.Shared.Atmos;

namespace Content.Server.Explosion.EntitySystems;

/// <summary>
///     This is the base class for <see cref="SpaceExplosion"/> and <see cref="GridExplosion"/>. It just exists to avoid some code duplication, because those classes are generally quite distinct.
/// </summary>
internal abstract class TileExplosion
{
    // Main tile data sets, mapping iterations onto tile lists
    internal Dictionary<int, List<Vector2i>> TileLists = new();
    protected Dictionary<int, List<Vector2i>> BlockedTileLists = new();
    protected Dictionary<int, HashSet<Vector2i>> FreedTileLists = new();

    // The new tile lists added each iteration. I **could** just pass these along to every function, but IMO it is more
    // readable if they are just private variables.
    protected List<Vector2i> NewTiles = default!;
    protected List<Vector2i> NewBlockedTiles = default!;
    protected HashSet<Vector2i> NewFreedTiles = default!;

    // HashSets used to ensure uniqueness of tiles. Prevents the explosion from looping back in on itself.
    protected HashSet<Vector2i> ProcessedTiles = new();
    protected HashSet<Vector2i> UnenteredBlockedTiles = new();
    protected HashSet<Vector2i> EnteredBlockedTiles = new();

    internal virtual void InitTile(Vector2i initialTile)
    {
        ProcessedTiles.Add(initialTile);
        TileLists[0] = new() { initialTile };
    }

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
    internal void CleanUp()
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
