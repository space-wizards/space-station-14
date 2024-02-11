using Robust.Shared.Serialization;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    public sealed class DecalChunkUpdateEvent : EntityEventArgs
    {
        public Dictionary<NetEntity, Dictionary<Vector2i, DecalChunk>> Data = new();
        public Dictionary<NetEntity, HashSet<Vector2i>> RemovedChunks = new();
    }
}
