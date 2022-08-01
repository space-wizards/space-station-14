namespace Content.Server.NPC.Pathfinding;

[RegisterComponent]
[Access(typeof(PathfindingSystem))]
public sealed class GridPathfindingComponent : Component, IPathfindingGraph
{
    public Dictionary<Vector2i, PathfindingChunk> Graph = new();
}
