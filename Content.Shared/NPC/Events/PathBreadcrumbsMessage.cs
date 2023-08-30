using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public sealed class PathBreadcrumbsMessage : EntityEventArgs
{
    public Dictionary<EntityUid, Dictionary<Vector2i, List<PathfindingBreadcrumb>>> Breadcrumbs = new();
}

[Serializable, NetSerializable]
public sealed class PathBreadcrumbsRefreshMessage : EntityEventArgs
{
    public EntityUid GridUid;
    public Vector2i Origin;
    public List<PathfindingBreadcrumb> Data = new();
}

[Serializable, NetSerializable]
public sealed class PathPolysMessage : EntityEventArgs
{
    public Dictionary<EntityUid, Dictionary<Vector2i, Dictionary<Vector2i, List<DebugPathPoly>>>> Polys = new();
}
