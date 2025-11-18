using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public sealed class PathPolysRefreshMessage : EntityEventArgs
{
    public NetEntity GridUid;
    public Vector2i Origin;

    /// <summary>
    /// Multi-dimension arrays aren't supported so
    /// </summary>
    public Dictionary<Vector2i, List<DebugPathPoly>> Polys = new();
}
