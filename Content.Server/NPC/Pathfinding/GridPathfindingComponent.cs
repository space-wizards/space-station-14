using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.NPC.Pathfinding;

/// <summary>
/// Stores the relevant pathfinding data for grids.
/// </summary>
[RegisterComponent, Access(typeof(PathfindingSystem))]
public sealed class GridPathfindingComponent : Component
{
    [ViewVariables]
    public readonly HashSet<Vector2i> DirtyChunks = new();

    /// <summary>
    /// Next time the graph is allowed to update.
    /// </summary>
    [ViewVariables, DataField("nextUpdate", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    [ViewVariables]
    public readonly Dictionary<Vector2i, GridPathfindingChunk> Chunks = new();

    /// <summary>
    /// Retrieves the chunk where the specified portal is stored on this grid.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<PathPortal, Vector2i> PortalLookup = new();

    [ViewVariables]
    public readonly List<PathPortal> DirtyPortals = new();
}
