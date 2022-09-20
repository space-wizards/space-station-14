using Robust.Shared.Map;

namespace Content.Shared.NPC;

/// <summary>
/// Stores the in-progress start of a pathfinding request.
/// </summary>
public sealed class PathfindingJob : EntityEventArgs
{
    public EntityCoordinates Start;
    public EntityCoordinates End;

    public Queue<PathPoly> Path;

    public bool Partial = false;

    public PathResult Result;

    public PathfindingJob(EntityCoordinates start, EntityCoordinates end, Queue<PathPoly> path)
    {
        Start = start;
        End = end;
        Path = path;
    }
}

