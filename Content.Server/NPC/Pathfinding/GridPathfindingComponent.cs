namespace Content.Server.NPC.Pathfinding;

[RegisterComponent]
[Access(typeof(PathfindingSystem))]
public sealed class GridPathfindingComponent : Component, IPathfindingGraph
{
    public readonly Dictionary<Vector2i, PathfindingChunk> Graph = new();
}
