using Content.Shared.Atmos;
using Robust.Shared.Map;
namespace Content.Server.Explosion.EntitySystems;

// This partial part of the explosion system has all of the functions used to facilitate explosions moving across grids.
// A good portion of it is focused around keeping track of what tile-indices on a grid correspond to tiles that border
// space. AFAIK no other system currently needs to track these "edge-tiles". If they do, this should probably be a
// property of the grid itself?
public sealed partial class ExplosionSystem : EntitySystem
{
    private void OnGridRemoved(GridRemovalEvent ev)
    {
        _airtightMap.Remove(ev.EntityUid);
    }

    /// <summary>
    ///     Given an grid-edge blocking map, check if the blockers are allowed to propagate to each other through gaps in grids.
    /// </summary>
    /// <remarks>
    ///     After grid edges were transformed into the reference frame of some other grid, this function figures out
    ///     which of those edges are actually blocking explosion propagation.
    /// </remarks>
    public void GetUnblockedDirections(Dictionary<Vector2i, BlockedSpaceTile> transformedEdges, float tileSize)
    {
        foreach (var (tile, data) in transformedEdges)
        {
            if (data.UnblockedDirections == AtmosDirection.Invalid)
                continue; // already all blocked.

            var tileCenter = ((Vector2) tile + 0.5f) * tileSize;
            foreach (var edge in data.BlockingGridEdges)
            {
                // if a blocking edge contains the center of the tile, block all directions
                if (edge.Box.Contains(tileCenter))
                {
                    data.UnblockedDirections = AtmosDirection.Invalid;
                    break;
                }

                // check north
                if (edge.Box.Contains(tileCenter + (0, tileSize / 2f)))
                    data.UnblockedDirections &= ~AtmosDirection.North;

                // check south
                if (edge.Box.Contains(tileCenter + (0, -tileSize / 2f)))
                    data.UnblockedDirections &= ~AtmosDirection.South;

                // check east
                if (edge.Box.Contains(tileCenter + (tileSize / 2f, 0)))
                    data.UnblockedDirections &= ~AtmosDirection.East;

                // check west
                if (edge.Box.Contains(tileCenter + (-tileSize / 2f, 0)))
                    data.UnblockedDirections &= ~AtmosDirection.West;
            }
        }
    }

    // yeah this is now the third direction flag enum, and the 5th (afaik) direction enum overall.....
    /// <summary>
    ///     Directional bitflags used to denote the neighbouring tiles of some tile on a grid.. Differ from atmos and
    ///     normal directional flags as NorthEast != North | East
    /// </summary>
    [Flags]
    public enum NeighborFlag : byte
    {
        Invalid = 0,
        North = 1 << 0,
        NorthEast = 1 << 1,
        East = 1 << 2,
        SouthEast = 1 << 3,
        South = 1 << 4,
        SouthWest = 1 << 5,
        West = 1 << 6,
        NorthWest = 1 << 7,

        Cardinal = North | East | South | West,
        Diagonal = NorthEast | SouthEast | SouthWest | NorthWest,
        Any = Cardinal | Diagonal
    }

    public static bool AnyNeighborBlocked(NeighborFlag neighbors, AtmosDirection blockedDirs)
    {
        if ((neighbors & NeighborFlag.North) == NeighborFlag.North && (blockedDirs & AtmosDirection.North) == AtmosDirection.North)
            return true;

        if ((neighbors & NeighborFlag.South) == NeighborFlag.South && (blockedDirs & AtmosDirection.South) == AtmosDirection.South)
            return true;

        if ((neighbors & NeighborFlag.East) == NeighborFlag.East && (blockedDirs & AtmosDirection.East) == AtmosDirection.East)
            return true;

        if ((neighbors & NeighborFlag.West) == NeighborFlag.West && (blockedDirs & AtmosDirection.West) == AtmosDirection.West)
            return true;

        return false;
    }

    // array indices match NeighborFlags shifts.
    public static readonly Vector2i[] NeighbourVectors =
        {
            new (0, 1),
            new (1, 1),
            new (1, 0),
            new (1, -1),
            new (0, -1),
            new (-1, -1),
            new (-1, 0),
            new (-1, 1)
        };
}
