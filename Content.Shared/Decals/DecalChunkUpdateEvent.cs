using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    public sealed class DecalChunkUpdateEvent : EntityEventArgs
    {
        public Dictionary<EntityUid, Dictionary<Vector2i, Dictionary<uint, Decal>>> Data = new();
        public Dictionary<EntityUid, HashSet<Vector2i>> RemovedChunks = new();
    }
}
