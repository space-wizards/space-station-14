namespace Content.Shared.NPC;

[Flags]
public enum PathfindingDebugMode : byte
{
    None = 0,

    /// <summary>
    /// Show the pathfinding breadcrumbs that are exterior.
    /// NOT the breadcrumbs that are considered outside of the chunk boundary.
    /// </summary>
    Boundary = 1 << 0,

    /// <summary>
    /// Show the individual pathfinding breadcrumbs.
    /// </summary>
    Breadcrumbs = 1 << 1,

    /// <summary>
    /// Show the pathfinding chunk edges.
    /// </summary>
    Chunks = 1 << 2,

    /// <summary>
    /// Shows the stats nearest crumb to the mouse cursor.
    /// </summary>
    Crumb = 1 << 3,

    /// <summary>
    /// Show the external edges being used for triangulation.
    /// </summary>
    Edges = 1 << 4,
}
