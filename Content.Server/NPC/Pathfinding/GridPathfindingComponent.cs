namespace Content.Server.NPC.Pathfinding;

/// <summary>
/// Stores the relevant pathfinding data for grids.
/// </summary>
[RegisterComponent, Access(typeof(PathfindingSystem))]
public sealed class GridPathfindingComponent : Component
{
    public readonly HashSet<Vector2i> DirtyChunks = new();

    /// <summary>
    /// Next time the graph is allowed to update.
    /// </summary>
    public TimeSpan NextUpdate;

    public readonly Dictionary<Vector2i, GridPathfindingChunk> Chunks = new();

    /// <summary>
    /// Retrieves the chunk where the specified portal is stored on this grid.
    /// </summary>
    public readonly Dictionary<PathPortal, Vector2i> PortalLookup = new();

    public readonly List<PathPortal> DirtyPortals = new();
}
