using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

/// <summary>
/// Debug message containing a pathfinding route.
/// </summary>
[Serializable, NetSerializable]
public sealed class PathRouteMessage : EntityEventArgs
{
    public List<PathPoly> Path;
    public Dictionary<PathPoly, float> Costs;

    public PathRouteMessage(List<PathPoly> path, Dictionary<PathPoly, float> costs)
    {
        Path = path;
        Costs = costs;
    }
}
