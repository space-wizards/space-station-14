namespace Content.Server.FloodFill.Data;

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
