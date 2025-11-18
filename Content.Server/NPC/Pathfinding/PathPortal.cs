using Robust.Shared.Map;

namespace Content.Server.NPC.Pathfinding;

/// <summary>
/// Connects 2 disparate locations.
/// </summary>
/// <remarks>
/// For example, 2 docking airlocks connecting 2 graphs, or an actual portal on the same graph.
/// </remarks>
public struct PathPortal
{
    // Assume for now it's 2-way and code 1-ways later.
    public readonly int Handle;
    public readonly EntityCoordinates CoordinatesA;
    public readonly EntityCoordinates CoordinatesB;

    // TODO: Whenever the chunk rebuilds need to add a neighbor.
    public PathPortal(int handle, EntityCoordinates coordsA, EntityCoordinates coordsB)
    {
        Handle = handle;
        CoordinatesA = coordsA;
        CoordinatesB = coordsB;
    }

    public override int GetHashCode()
    {
        return Handle;
    }
}
