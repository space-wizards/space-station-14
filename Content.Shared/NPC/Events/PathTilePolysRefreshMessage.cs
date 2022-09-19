using Robust.Shared.Serialization;

namespace Content.Shared.NPC;

[Serializable, NetSerializable]
public sealed class PathTilePolysRefreshMessage : EntityEventArgs
{
    public EntityUid GridUid;
    public Vector2i Origin;

    /// <summary>
    /// Multi-dimension arrays aren't supported so
    /// </summary>
    public Dictionary<Vector2i, List<Box2i>> Polys = new();
}
