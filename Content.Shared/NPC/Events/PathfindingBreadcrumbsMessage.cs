using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public sealed class PathfindingBreadcrumbsMessage : EntityEventArgs
{
    public Dictionary<EntityUid, Dictionary<Vector2i, List<PathfindingBreadcrumb>>> Breadcrumbs = new();
}
