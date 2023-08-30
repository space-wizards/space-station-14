using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

/// <summary>
/// Boundary around a navigation region.
/// </summary>
[Serializable, NetSerializable]
public struct PathfindingBoundary
{
    public List<PathfindingBreadcrumb> Breadcrumbs;

    /// <summary>
    /// Is it a closed loop or is it a special-case chain (e.g. thindows).
    /// </summary>
    public bool Closed;

    public PathfindingBoundary(bool closed, List<PathfindingBreadcrumb> crumbs)
    {
        Closed = closed;
        Breadcrumbs = crumbs;
    }
}
