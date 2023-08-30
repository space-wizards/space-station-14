using Robust.Shared.Serialization;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    public sealed class DecalChunkUpdateEvent : EntityEventArgs
    {
        public Dictionary<EntityUid, Dictionary<Vector2i, DecalChunk>> Data = new();
        public Dictionary<EntityUid, HashSet<Vector2i>> RemovedChunks = new();
    }
}
