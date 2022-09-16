namespace Content.Server.NPC.Pathfinding;

/// <summary>
/// Stores the relevant pathfinding data for grids.
/// </summary>
[RegisterComponent]
[Access(typeof(PathfindingSystem))]
public sealed class GridPathfindingComponent : Component
{
    public readonly Dictionary<Vector2i, GridPathfindingChunk> Chunks = new();
}

public sealed class GridPathfindingChunk
{
    public readonly GridPathfindingPoint[] Points = new GridPathfindingPoint[
        PathfindingSystem.ChunkSize * PathfindingSystem.SubStep *
        PathfindingSystem.ChunkSize * PathfindingSystem.SubStep];

    public void Clear()
    {
        Array.Clear(Points);
    }
}

public struct GridPathfindingPoint
{
    public Vector2 Coordinates;
    public bool IsSpace;
    public int CollisionLayer;
    public int CollisionMask;
}
