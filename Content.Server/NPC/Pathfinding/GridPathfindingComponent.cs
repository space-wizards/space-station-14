using Content.Shared.NPC;

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
    public readonly PathfindingBreadcrumb[,] Points = new PathfindingBreadcrumb[
        SharedPathfindingSystem.ChunkSize * SharedPathfindingSystem.SubStep,
        SharedPathfindingSystem.ChunkSize * SharedPathfindingSystem.SubStep];

    public Vector2i Origin;

    public void Clear()
    {
        Array.Clear(Points);
    }
}
