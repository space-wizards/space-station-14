namespace Content.Shared.NPC;

[Flags]
public enum PathfindingDebugMode : ushort
{
    None = 0,

    /// <summary>
    /// Show the individual pathfinding breadcrumbs.
    /// </summary>
    Breadcrumbs = 1 << 0,

    /// <summary>
    /// Show the pathfinding chunk edges.
    /// </summary>
    Chunks = 1 << 1,

    /// <summary>
    /// Shows the stats nearest crumb to the mouse cursor.
    /// </summary>
    Crumb = 1 << 2,

    /// <summary>
    /// Shows all of the pathfinding polys.
    /// </summary>
    Polys = 1 << 6,

    /// <summary>
    /// Shows the edges between pathfinding polys.
    /// </summary>
    PolyNeighbors = 1 << 7,

    /// <summary>
    /// Shows the nearest poly to the mouse cursor.
    /// </summary>
    Poly = 1 << 8,

    /// <summary>
    /// Gets a path from the current attached entity to the mouse cursor.
    /// </summary>
    Path = 1 << 9,

    Routes = 1 << 10,

    RouteCosts = 1 << 11,
}
