using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public sealed class PathfindingBreadcrumbsMessage : EntityEventArgs
{
    public Dictionary<EntityUid, Dictionary<Vector2i, List<PathfindingBreadcrumb>>> Breadcrumbs = new();
}

[Serializable, NetSerializable]
public sealed class PathfindingBreadcrumbsRefreshMessage : EntityEventArgs
{
    public EntityUid GridUid;
    public Vector2i Origin;
    public List<PathfindingBreadcrumb> Data = new();
}
