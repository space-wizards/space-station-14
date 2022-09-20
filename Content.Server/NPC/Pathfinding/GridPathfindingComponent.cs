using Content.Shared.NPC;

namespace Content.Server.NPC.Pathfinding;

/// <summary>
/// Stores the relevant pathfinding data for grids.
/// </summary>
[RegisterComponent]
public sealed class GridPathfindingComponent : Component
{
    public readonly HashSet<Vector2i> DirtyChunks = new();
    public TimeSpan NextUpdate;

    public readonly Dictionary<Vector2i, GridPathfindingChunk> Chunks = new();

    public PathPoly GetNeighbor(PathPolyRef neighbor)
    {
        var tileX = neighbor.Index / SharedPathfindingSystem.ChunkSize;
        var tileY = neighbor.Index % SharedPathfindingSystem.ChunkSize;

        return Chunks[neighbor.ChunkOrigin].Polygons[tileX, tileY][neighbor.TileIndex];
    }
}

public sealed class GridPathfindingChunk
{
    public readonly PathfindingBreadcrumb[,] Points = new PathfindingBreadcrumb[
        (SharedPathfindingSystem.ChunkSize) * SharedPathfindingSystem.SubStep,
        (SharedPathfindingSystem.ChunkSize) * SharedPathfindingSystem.SubStep];

    public Vector2i Origin;

    public readonly List<PathPoly>[,] Polygons = new List<PathPoly>[SharedPathfindingSystem.ChunkSize, SharedPathfindingSystem.ChunkSize];

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

    public void Clear(GridPathfindingComponent component)
    {
        Array.Clear(Points);

        // Still clear internals for now as we don't gracefully handle anything mid-path
        for (byte x = 0; x < SharedPathfindingSystem.ChunkSize; x++)
        {
            for (byte y = 0; y < SharedPathfindingSystem.ChunkSize; y++)
            {
                var tilePolys = Polygons[x, y];

                for (byte i = 0; i < tilePolys.Count; i++)
                {
                    var poly = tilePolys[i];
                    var index = (byte) (x * SharedPathfindingSystem.ChunkSize + y);

                    var nodeRef = new PathPolyRef()
                    {
                        ChunkOrigin = Origin,
                        Index = index,
                        TileIndex = i,
                    };

                    foreach (var neighborRef in poly.Neighbors)
                    {
                        var neighbor = component.GetNeighbor(neighborRef);
                        neighbor.Neighbors.Remove(nodeRef);
                    }

                    poly.Neighbors.Clear();
                }
            }
        }
    }
}
