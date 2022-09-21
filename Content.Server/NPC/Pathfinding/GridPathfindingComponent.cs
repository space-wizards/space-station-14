using Content.Shared.NPC;

namespace Content.Server.NPC.Pathfinding;

/// <summary>
/// Stores the relevant pathfinding data for grids.
/// </summary>
[RegisterComponent]
public sealed class GridPathfindingComponent : Component
{
    public readonly HashSet<Vector2i> DirtyChunks = new();

    /// <summary>
    /// Last time the graph updated.
    /// If this is greater than when a path request started then nodes may be out of date.
    /// </summary>
    public TimeSpan LastUpdate;

    /// <summary>
    /// Next time the graph is allowed to update.
    /// </summary>
    public TimeSpan NextUpdate;

    public readonly Dictionary<Vector2i, GridPathfindingChunk> Chunks = new();
}
