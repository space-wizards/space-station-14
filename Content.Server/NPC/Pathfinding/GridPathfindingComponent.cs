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
        (SharedPathfindingSystem.ChunkSize + SharedPathfindingSystem.ExpansionSize * 2) * SharedPathfindingSystem.SubStep,
        (SharedPathfindingSystem.ChunkSize + SharedPathfindingSystem.ExpansionSize * 2) * SharedPathfindingSystem.SubStep];

    public Vector2i Origin;

    public List<PathPoly>[,] Polygons = new List<PathPoly>[SharedPathfindingSystem.ChunkSize, SharedPathfindingSystem.ChunkSize];

    public GridPathfindingChunk()
    {
        for (var x = 0; x < Math.Sqrt(Polygons.Length); x++)
        {
            for (var y = 0; y < Math.Sqrt(Polygons.Length); y++)
            {
                Polygons[x, y] = new List<PathPoly>();
            }
        }
    }

    public void Clear()
    {
        Array.Clear(Points);

        // TODO: Only go on the outside polys
        foreach (var arr in Polygons)
        {
            foreach (var poly in arr)
            {
                foreach (var neighbor in poly.Neighbors)
                {
                    neighbor.Neighbors.Remove(poly);
                }
            }

            arr.Clear();
        }
    }
}
