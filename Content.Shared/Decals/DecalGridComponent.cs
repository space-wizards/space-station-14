using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using static Content.Shared.Decals.DecalGridComponent;

namespace Content.Shared.Decals
{
    [RegisterComponent]
    [Access(typeof(SharedDecalSystem))]
    [NetworkedComponent]
    public sealed class DecalGridComponent : Component
    {
        [DataField("chunkCollection", serverOnly: true)]
        public DecalGridChunkCollection ChunkCollection = new(new ());

        [Serializable, NetSerializable]
        public record DecalGridChunkCollection(Dictionary<Vector2i, Dictionary<uint, Decal>> ChunkCollection)
        {
            public uint NextUid;
        }
    }

    [Serializable, NetSerializable]
    public sealed class DecalGridState : ComponentState
    {
        public DecalGridChunkCollection ChunkCollection;

        // TODO properly support delta states??
        public DecalGridState(DecalGridComponent comp)
        {
            ChunkCollection = comp.ChunkCollection;
        }
    }

}
