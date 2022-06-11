namespace Content.Server.AI.Pathfinding;

[RegisterComponent]

public sealed class GridPathfindingComponent : Component, IPathfindingGraph
{
    public Dictionary<Vector2i, PathfindingChunk> Graph = new();
}
